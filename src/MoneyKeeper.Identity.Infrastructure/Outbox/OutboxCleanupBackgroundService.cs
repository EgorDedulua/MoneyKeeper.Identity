using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyKeeper.Identity.Application.Common.Interfaces.Outbox;
using MoneyKeeper.Identity.Application.Outbox;

namespace MoneyKeeper.Identity.Infrastructure.Outbox
{
    public class OutboxCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OutboxSettings _settings;
        private readonly ILogger<OutboxCleanupBackgroundService> _logger;

        public OutboxCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<OutboxSettings> options,
            ILogger<OutboxCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(_settings.CleanupIntervalHours));

            await RunCleanupCycleAsync(stoppingToken).ConfigureAwait(false);

            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RunCleanupCycleAsync(stoppingToken).ConfigureAwait(false);
            }
        }

        private async Task RunCleanupCycleAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IOutboxCleanupService>();

                int deletedInBatch;
                do
                {
                    deletedInBatch = await cleanupService.DeleteProcessedBatchAsync(stoppingToken).ConfigureAwait(false);
                }
                while (deletedInBatch > 0 && !stoppingToken.IsCancellationRequested);

                await cleanupService.ReportStaleMessagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Необработанная ошибка в цикле очистки outbox-таблицы");
            }
        }
    }
}
