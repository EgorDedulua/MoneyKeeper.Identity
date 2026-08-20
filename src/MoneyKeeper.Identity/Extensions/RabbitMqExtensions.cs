using Microsoft.Extensions.Options;
using MoneyKeeper.Identity.Application.Common.Interfaces.Messaging;
using MoneyKeeper.Identity.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace MoneyKeeper.Identity.Extensions
{
    public static class RabbitMQExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConnection>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("RabbitMqConnectionFactory");
                IConnection connection = CreateConnection(settings, logger);
                return connection;
            });

            services.AddScoped<IMessageBus, RabbitMqMessageBus>();

            return services;
        }

        private static IConnection CreateConnection(RabbitMqSettings settings, ILogger logger)
        {
            int attempt = 0;
            Exception? lastException = null;

            while(attempt < settings.InitialConnectionRetryCount)
            {
                ++attempt;
                try
                {
                    ConnectionFactory connectionFactory = new ConnectionFactory
                    {
                        HostName = settings.HostName,
                        Port = settings.Port,
                        UserName = settings.UserName,
                        Password = settings.Password,
                        VirtualHost = settings.VirtualHost,
                        AutomaticRecoveryEnabled = settings.AutomaticRecoveryEnabled,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(settings.NetworkRecoveryIntervalSeconds),
                        RequestedHeartbeat = TimeSpan.FromSeconds(settings.RequestedHeartbeatSeconds),
                        TopologyRecoveryEnabled = true
                    };

                    IConnection connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
                    logger.LogInformation("Соединение с RabbitMQ установлено (попытка {Attempt})", attempt);

                    InitializeTopology(connection, settings, logger).GetAwaiter().GetResult();

                    connection.RecoverySucceededAsync += async (sender, args) =>
                    {
                        logger.LogInformation("Соединение с RabbitMQ восстановлено. Повторная инициализация топологии...");
                        try
                        {
                            await InitializeTopology(connection, settings, logger);
                        }
                        catch (Exception ex)
                        {
                            logger.LogCritical(ex, "Не удалось повторно инициализировать топологию после восстановления соединения");
                        }
                    };

                    connection.ConnectionRecoveryErrorAsync += async (sender, args) =>
                    {
                        logger.LogError(args.Exception, "Ошибка восстановления соединения с RabbitMQ");
                        await Task.CompletedTask;
                    };

                    return connection;
                }
                catch(Exception ex)
                {
                    lastException = ex;
                    logger.LogWarning(ex, "Не удалось создать соединение с RabbitMQ. Попытка {Attempt} из {RetryCount}"
                        , attempt, settings.InitialConnectionRetryCount);

                    if (attempt < settings.InitialConnectionRetryCount)
                    {
                        Thread.Sleep(settings.InitialConnectionRetryDelayMilliseconds);
                    }
                }
            }

            logger.LogCritical(lastException, "Не удалось создать соединение с RabbitMQ после {RetryCount} попыток."
                , settings.InitialConnectionRetryCount);
            throw new InvalidOperationException("Не удалось инициализировать RabbitMQ соединение", lastException);
        }

        private static async Task InitializeTopology(IConnection connection, RabbitMqSettings settings, ILogger logger)
        {
            await using var channel = await connection.CreateChannelAsync().ConfigureAwait(false);

            await channel.ExchangeDeclareAsync(
                exchange: settings.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false
            ).ConfigureAwait(false);

            await channel.ExchangeDeclareAsync(
               exchange: settings.DlExchange,
               type: ExchangeType.Direct,
               durable: true,
               autoDelete: false
            ).ConfigureAwait(false);

            await channel.QueueDeclareAsync(
                queue: settings.DlQueue,
                durable: true,
                exclusive: false,
                autoDelete: false
            ).ConfigureAwait(false);

            await channel.QueueBindAsync(
                queue: settings.DlQueue,
                exchange: settings.DlExchange,
                routingKey: settings.DlqRoutingKey
            ).ConfigureAwait(false);

            logger.LogInformation("Топология RabbitMQ инициализирована");
        }
    }
}
