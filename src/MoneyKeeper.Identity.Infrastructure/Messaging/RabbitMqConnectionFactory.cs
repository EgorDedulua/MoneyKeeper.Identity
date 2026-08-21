using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MoneyKeeper.Identity.Infrastructure.Messaging
{
    public class RabbitMqConnectionFactory
    {
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<RabbitMqConnectionFactory> _logger;

        public RabbitMqConnectionFactory(IOptions<RabbitMqSettings> options, ILogger<RabbitMqConnectionFactory> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            Exception? lastException = null;

            while(attempt < _settings.InitialConnectionRetryCount)
            {
                ++attempt;
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    ConnectionFactory connectionFactory = new ConnectionFactory
                    {
                        HostName = _settings.HostName,
                        Port = _settings.Port,
                        UserName = _settings.UserName,
                        Password = _settings.Password,
                        VirtualHost = _settings.VirtualHost,
                        AutomaticRecoveryEnabled = _settings.AutomaticRecoveryEnabled,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(_settings.NetworkRecoveryIntervalSeconds),
                        RequestedHeartbeat = TimeSpan.FromSeconds(_settings.RequestedHeartbeatSeconds),
                        TopologyRecoveryEnabled = true,
                        ContinuationTimeout = TimeSpan.FromSeconds(_settings.ContinuationTimeoutSeconds)
                    };

                    _logger.LogInformation("Попытка {Attempt} из {RetryCount} подключения к RabbitMQ: {Host}:{Port}",
                        attempt, _settings.InitialConnectionRetryCount, _settings.HostName, _settings.Port);

                    IConnection connection = await connectionFactory.CreateConnectionAsync(cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation("Соединение с RabbitMQ установлено (попытка {Attempt})", attempt);

                    return connection;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Установка соединения с RabbitMQ отменена вызывающим кодом");
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Не удалось создать соединение с RabbitMQ. Попытка {Attempt} из {RetryCount}",
                        attempt, _settings.InitialConnectionRetryCount);

                    if (attempt < _settings.InitialConnectionRetryCount)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(_settings.InitialConnectionRetryDelayMilliseconds), cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            _logger.LogCritical(lastException, "Не удалось создать соединение с RabbitMQ после {RetryCount} попыток",
                _settings.InitialConnectionRetryCount);

            throw new InvalidOperationException("Не удалось инициализировать RabbitMQ соединение", lastException);
        }
    }
}
