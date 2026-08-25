using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;

namespace MoneyKeeper.Identity.Infrastructure.Messaging
{
    public class RabbitMqConnectionFactory
    {
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<RabbitMqConnectionFactory> _logger;
        private readonly ResiliencePipeline<IConnection> _pipeline;

        public RabbitMqConnectionFactory(IOptions<RabbitMqSettings> options, ILogger<RabbitMqConnectionFactory> logger)
        {
            _settings = options.Value;
            _logger = logger;
            _pipeline = BuildPipeline();
        }

        private ResiliencePipeline<IConnection> BuildPipeline()
        {
            return new ResiliencePipelineBuilder<IConnection>()
                .AddRetry(new RetryStrategyOptions<IConnection>
                {
                    ShouldHandle = new PredicateBuilder<IConnection>()
                        .Handle<Exception>(ex => ex is not BrokenCircuitException),
                    MaxRetryAttempts = _settings.InitialConnectionRetryCount,
                    Delay = TimeSpan.FromMilliseconds(_settings.InitialConnectionRetryDelayMilliseconds),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        _logger.LogWarning(args.Outcome.Exception, "Не удалось создать соединение с RabbitMQ. " +
                            "Попытка {Attempt} из {RetryCount}.",
                            args.AttemptNumber + 1, _settings.InitialConnectionRetryCount);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<IConnection>
                {
                    ShouldHandle = new PredicateBuilder<IConnection>().Handle<Exception>(),
                    FailureRatio = _settings.CircuitBreakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(_settings.CircuitBreakerSamplingDurationSeconds),
                    MinimumThroughput = _settings.CircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(_settings.CircuitBreakerBreakDurationSeconds),
                    OnOpened = args =>
                    {
                        _logger.LogCritical("Circuit breaker для подключений к RabbitMQ РАЗОМКНУТ. Причина: {Reason}",
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        _logger.LogInformation("Circuit breaker для подключений к RabbitMQ снова ЗАМКНУТ — соединение восстановлено");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = _ =>
                    {
                        _logger.LogInformation("Circuit breaker для подключений к RabbitMQ в состоянии HALF-OPENED — пробная попытка");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _pipeline.ExecuteAsync(async ct =>
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

                    _logger.LogInformation("Подключение к RabbitMQ: {Host}:{Port}", _settings.HostName, _settings.Port);

                    IConnection connection = await connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);

                    _logger.LogInformation("Соединение с RabbitMQ установлено");

                    return connection;
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogCritical(ex, "RabbitMQ считается недоступным (circuit breaker разомкнут) — подключение не выполнялось");
                throw new InvalidOperationException("RabbitMQ недоступен (circuit breaker разомкнут)", ex);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Не удалось создать соединение с RabbitMQ после {RetryCount} попыток",
                    _settings.InitialConnectionRetryCount);
                throw new InvalidOperationException("Не удалось инициализировать RabbitMQ соединение", ex);
            }
        }
    }
}
