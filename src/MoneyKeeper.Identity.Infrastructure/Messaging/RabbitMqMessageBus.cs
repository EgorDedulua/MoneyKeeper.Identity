using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyKeeper.Identity.Application.Common.Interfaces.Messaging;
using MoneyKeeper.Identity.Application.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace MoneyKeeper.Identity.Infrastructure.Messaging
{
    public class RabbitMqMessageBus : IMessageBus
    {
        private readonly IConnection _connection;
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<RabbitMqMessageBus> _logger;

        public RabbitMqMessageBus(IConnection connection, IOptions<RabbitMqSettings> options, ILogger<RabbitMqMessageBus> logger)
        {
            _connection = connection;
            _settings = options.Value;
            _logger = logger;
        }

        public Task PublishUserRegisteredAsync(UserRegisteredEvent message, CancellationToken cancellationToken = default)
            => PublishAsync(message, _settings.UserRegisteredRoutingKey, cancellationToken);

        public Task PublishUserDeletedAsync(UserDeletedEvent message, CancellationToken cancellationToken = default)
            => PublishAsync(message, _settings.UserDeletedRoutingKey, cancellationToken);

        private async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();

            byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);
            string messageId = Guid.NewGuid().ToString();
            BasicProperties properties = CreateBasicProperties(messageId, typeof(T).Name);

            using var publishCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            publishCts.CancelAfter(TimeSpan.FromSeconds(_settings.PublishTimeoutSeconds));

            int attempt = 0;
            Exception? lastException = null;

            while (attempt < _settings.RetryCount)
            {
                ++attempt;
                try
                {
                    await using var channel = await _connection.CreateChannelAsync(
                        new CreateChannelOptions(
                            publisherConfirmationsEnabled: true,
                            publisherConfirmationTrackingEnabled: true
                        ),
                        cancellationToken: publishCts.Token
                    ).ConfigureAwait(false);

                    channel.BasicReturnAsync += async (sender, args) =>
                        await HandleBasicReturnAsync(args).ConfigureAwait(false);

                    await channel.BasicPublishAsync(
                        exchange: _settings.ExchangeName,
                        routingKey: routingKey,
                        mandatory: true,
                        basicProperties: properties,
                        body: body,
                        cancellationToken: publishCts.Token
                    ).ConfigureAwait(false);

                    _logger.LogInformation("Сообщение {MessageId} типа {MessageType} успешно опубликовано с routingKey {RoutingKey}",
                        messageId, typeof(T).Name, routingKey);

                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Публикация сообщения {MessageId} отменена вызывающим кодом.", messageId);
                    throw;
                }
                catch (OperationCanceledException) when (publishCts.IsCancellationRequested)
                {
                    lastException = new TimeoutException($"Таймаут публикации сообщения {messageId}.");
                    _logger.LogWarning(lastException, "Таймаут публикации сообщения {MessageId}. Попытка {Attempt}.", messageId, attempt);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Ошибка публикации сообщения {MessageId}. Попытка {Attempt} из {RetryCount}."
                        , messageId, attempt, _settings.RetryCount);
                }

                if (attempt < _settings.RetryCount)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Публикация сообщения {MessageId} отменена вызывающим кодом.", messageId);
                        throw;
                    }
                }
            }

            _logger.LogError(lastException, "Не удалось опубликовать сообщение {MessageId} после {RetryCount} попыток. " +
                "Попытка отправки в DLQ.", messageId, _settings.RetryCount);

            await TrySendToDlqAsync(
                originalBody: body,
                originalMessageId: messageId,
                originalExchange: _settings.ExchangeName,
                originalRoutingKey: routingKey,
                failureException: lastException,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            if (lastException is not null)
                throw new 
                    InvalidOperationException($"Не удалось опубликовать сообщение {messageId} в основную очередь", lastException);
        }

        private async Task HandleBasicReturnAsync(BasicReturnEventArgs args)
        {
            string originalMessageId = args.BasicProperties?.MessageId ?? "unknown";
            byte[] originalBody = args.Body.ToArray();

            _logger.LogError("Сообщение {MessageId} возвращено брокером. Exchange: {Exchange}" +
                ", RoutingKey: {RoutingKey}, Код: {ReplyCode}, Причина: {ReplyText}",
                originalMessageId, args.Exchange, args.RoutingKey, args.ReplyCode, args.ReplyText);

            await TrySendToDlqAsync(
                originalBody: originalBody,
                originalMessageId: originalMessageId,
                originalExchange: args.Exchange,
                originalRoutingKey: args.RoutingKey,
                failureException: null,
                cancellationToken: CancellationToken.None
            ).ConfigureAwait(false);
        }

        private async Task TrySendToDlqAsync(byte[] originalBody, string originalMessageId, string originalExchange,
            string originalRoutingKey, Exception? failureException, CancellationToken cancellationToken)
        {
            try
            {
                await using var dlqChannel = await _connection.CreateChannelAsync(
                    new CreateChannelOptions(
                        publisherConfirmationsEnabled: true,
                        publisherConfirmationTrackingEnabled: true
                    ),
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                var enrichedBody = new
                {
                    _type = "UndeliveredMessage",
                    OriginalMessage = Convert.ToBase64String(originalBody),
                    OriginalExchange = originalExchange,
                    OriginalRoutingKey = originalRoutingKey,
                    OriginalMessageId = originalMessageId,
                    FailureMessage = failureException?.Message ?? "Сообщение возвращено брокером",
                    FailureTime = DateTime.UtcNow
                };

                byte[] dlqBody = JsonSerializer.SerializeToUtf8Bytes(enrichedBody);

                BasicProperties dlqProperties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = $"DLQ-{originalMessageId}",
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    AppId = _settings.AppId,
                    Headers = new Dictionary<string, object?>
                    {
                        ["x-message-type"] = "UndeliveredMessage"
                    }
                };

                using var dlqCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                dlqCts.CancelAfter(TimeSpan.FromSeconds(_settings.DlqPublishTimeoutSeconds));

                await dlqChannel.BasicPublishAsync(
                    exchange: _settings.DlExchange,
                    routingKey: _settings.DlqRoutingKey,
                    mandatory: false,
                    basicProperties: dlqProperties,
                    body: dlqBody,
                    cancellationToken: dlqCts.Token
                ).ConfigureAwait(false);

                _logger.LogInformation("Сообщение {MessageId} успешно отправлено в DLQ", originalMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Не удалось отправить сообщение {MessageId} в DLQ. Сообщение потеряно", originalMessageId);
                throw;
            }
        }

        private BasicProperties CreateBasicProperties(string messageId, string messageType)
        {
            return new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = messageId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                AppId = _settings.AppId,
                Headers = new Dictionary<string, object?>
                {
                    ["x-message-type"] = messageType,
                    ["x-published-at"] = DateTimeOffset.UtcNow.ToString("O")
                }
            };
        }
    }
}