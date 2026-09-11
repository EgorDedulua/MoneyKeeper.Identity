using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyKeeper.Identity.Application.Common.Interfaces.Outbox;
using MoneyKeeper.Identity.Application.Outbox;

namespace MoneyKeeper.Identity.Infrastructure.Outbox
{
    public class OutboxPublisherBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OutboxSettings _settings;
        private readonly ILogger<OutboxPublisherBackgroundService> _logger;

        public OutboxPublisherBackgroundService(IServiceScopeFactory scopeFactory, IOptions<OutboxSettings> options,
            ILogger<OutboxPublisherBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollIntervalSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var publisherService = scope.ServiceProvider.GetRequiredService<IOutboxPublisherService>();

                    int published;
                    do
                    {
                        published = await publisherService.PublishPendingBatchAsync(stoppingToken).ConfigureAwait(false);
                    }
                    while (published > 0 && !stoppingToken.IsCancellationRequested);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Необработанная ошибка в цикле публикации outbox-сообщений");
                }
            }
        }
    }
}
