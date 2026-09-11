using MoneyKeeper.Identity.Application.Common.Interfaces.Outbox;
using MoneyKeeper.Identity.Application.Outbox;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Infrastructure.Data.Repositories;
using MoneyKeeper.Identity.Infrastructure.Outbox;

namespace MoneyKeeper.Identity.Extensions
{
    public static class OutboxExtensions
    {
        public static IServiceCollection AddOutbox(this IServiceCollection services)
        {
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<IOutboxPublisherService, OutboxPublisherService>();
            services.AddScoped<IOutboxCleanupService, OutboxCleanupService>();
            services.AddHostedService<OutboxPublisherBackgroundService>();
            services.AddHostedService<OutboxCleanupBackgroundService>();
            return services;
        }
    }
}
