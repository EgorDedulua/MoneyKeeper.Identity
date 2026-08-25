using MoneyKeeper.Identity.Application.Common.Interfaces.Messaging;
using MoneyKeeper.Identity.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace MoneyKeeper.Identity.Extensions
{
    public static class RabbitMqExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<RabbitMqConnectionFactory>();
            services.AddSingleton<IConnection>(sp =>
            {
                var factory = sp.GetRequiredService<RabbitMqConnectionFactory>();
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
            services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
            services.AddHostedService<RabbitMqTopologyInitializer>();

            return services;
        }

    }
}
