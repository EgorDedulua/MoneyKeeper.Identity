using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MoneyKeeper.Identity.Infrastructure.Messaging
{
    public class RabbitMqTopologyInitializer : IHostedService
    {
        private readonly IConnection _connection;
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<RabbitMqTopologyInitializer> _logger;

        public RabbitMqTopologyInitializer(IConnection connection, IOptions<RabbitMqSettings> options, 
            ILogger<RabbitMqTopologyInitializer> logger)
        {
            _connection = connection;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeTopologyAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Топология RabbitMQ инициализирована");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Критическая ошибка при иницализации топологии RabbitMQ");
                throw;
            }

            _connection.RecoverySucceededAsync += OnConnectionRecoveredAsync;
            _connection.ConnectionRecoveryErrorAsync += OnConnectionRecoveryErrorAsync;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _connection.RecoverySucceededAsync -= OnConnectionRecoveredAsync;
            _connection.ConnectionRecoveryErrorAsync -= OnConnectionRecoveryErrorAsync;
            return Task.CompletedTask;
        }

        private async Task InitializeTopologyAsync(CancellationToken cancellationToken)
        {
            await using var channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true
                ),
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.ExchangeDeclareAsync(
                exchange: _settings.DlExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.QueueDeclareAsync(
                queue: _settings.UserDeletedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.QueueDeclareAsync(
                queue: _settings.UserRegisteredQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.QueueDeclareAsync(
                queue: _settings.DlQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.QueueBindAsync(
                queue: _settings.UserDeletedQueue,
                exchange: _settings.ExchangeName,
                routingKey: _settings.UserDeletedRoutingKey,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.QueueBindAsync(
                queue: _settings.UserRegisteredQueue,
                exchange: _settings.ExchangeName,
                routingKey: _settings.UserRegisteredRoutingKey,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await channel.QueueBindAsync(
                queue: _settings.DlQueue,
                exchange: _settings.DlExchange,
                routingKey: _settings.DlqRoutingKey,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            _logger.LogInformation("Топология RabbitMQ инициализирована: Exchange={Exchange}, DeadLetterExchange={DlExchange}, " +
                "Queues=[{Queue1}, {Queue2}, {Queue3}] ", _settings.ExchangeName, _settings.DlExchange,
                _settings.DlQueue, _settings.UserDeletedQueue, _settings.UserRegisteredQueue);
        }

        private async Task OnConnectionRecoveredAsync(object sender, AsyncEventArgs e)
        {
            _logger.LogWarning("Соединение с RabbitMQ восстановлено. Повторная инициализация топологии...");

            try
            {
                await InitializeTopologyAsync(CancellationToken.None).ConfigureAwait(false);
                _logger.LogInformation("Топология RabbitMQ повторно инициализирована после восстановления");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Не удалось повторно инициализировать топологию после восстановления соединения");
            }
        }

        private Task OnConnectionRecoveryErrorAsync(object sender, AsyncEventArgs e)
        {
            _logger.LogError("Ошибка восстановления соединения с RabbitMQ");
            return Task.CompletedTask;
        }
    }
}
