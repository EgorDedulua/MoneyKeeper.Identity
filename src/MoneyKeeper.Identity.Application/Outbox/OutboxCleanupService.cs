using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyKeeper.Identity.Application.Common.Interfaces.Outbox;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Application.Outbox
{
    public class OutboxCleanupService : IOutboxCleanupService
    {
        private readonly IOutboxRepository _outboxRepository;
        private readonly OutboxSettings _settings;
        private readonly ILogger<OutboxCleanupService> _logger;

        public OutboxCleanupService(IOutboxRepository outboxRepository, IOptions<OutboxSettings> options,
            ILogger<OutboxCleanupService> logger)
        {
            _outboxRepository = outboxRepository;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<int> DeleteProcessedBatchAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(-_settings.RetainProcessedDays);

            int deleted = await _outboxRepository
                .DeleteProcessedBatchAsync(cutoff, _settings.DeleteBatchSize, cancellationToken)
                .ConfigureAwait(false);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Удалено {Count} доставленных outbox-сообщений старше {RetainDays} дней",
                    deleted, _settings.RetainProcessedDays);
            }

            return deleted;
        }

        public async Task ReportStaleMessagesAsync(CancellationToken cancellationToken = default)
        {
            var staleBefore = DateTime.UtcNow.AddHours(-_settings.StaleThresholdHours);

            List<OutboxMessage> staleMessages = await _outboxRepository
                .GetStaleUnprocessedAsync(staleBefore, _settings.StaleBatchLimit, cancellationToken)
                .ConfigureAwait(false);

            if (staleMessages.Count == 0)
                return;

            foreach (OutboxMessage message in staleMessages)
            {
                _logger.LogCritical(
                    "Outbox-сообщение {MessageId} типа {MessageType} создано {CreatedAtUtc} и до сих пор не обработано " +
                    "(попыток: {AttemptCount}, IsAbandoned: {IsAbandoned}). Требуется проверка.",
                    message.Id, message.MessageType, message.CreatedAt, message.AttemptCount, message.IsAbandoned);
            }
        }
    }
}
