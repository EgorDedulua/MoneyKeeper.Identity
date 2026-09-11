using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Common.Interfaces.Outbox;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Application.Outbox
{
    public class OutboxPublisherService : IOutboxPublisherService
    {
        private readonly IOutboxRepository _outboxRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageBus _messageBus;
        private readonly OutboxSettings _settings;
        private readonly ILogger<OutboxPublisherService> _logger;

        public OutboxPublisherService(IOutboxRepository outboxRepository, IUnitOfWork unitOfWork, IMessageBus messageBus,
            IOptions<OutboxSettings> options, ILogger<OutboxPublisherService> logger)
        {
            _outboxRepository = outboxRepository;
            _unitOfWork = unitOfWork;
            _messageBus = messageBus;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<int> PublishPendingBatchAsync(CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                List<OutboxMessage> messages = await _outboxRepository
                    .GetPendingBatchForUpdateAsync(_settings.BatchSize, cancellationToken)
                    .ConfigureAwait(false);

                if (messages.Count == 0)
                {
                    await _unitOfWork.CommitTransactionAsync().ConfigureAwait(false);
                    return 0;
                }

                int publishedCount = 0;

                foreach (OutboxMessage message in messages)
                {
                    try
                    {
                        await _messageBus.PublishAsync(
                            messageId: message.Id,
                            messageType: message.MessageType,
                            payload: message.Payload,
                            cancellationToken: cancellationToken
                        ).ConfigureAwait(false);

                        message.ProcessedAt = DateTime.UtcNow;
                        ++publishedCount;
                    }
                    catch (Exception ex)
                    {
                        message.AttemptCount++;
                        message.LastAttemptAt = DateTime.UtcNow;
                        message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

                        if (message.AttemptCount > _settings.MaxAttempts)
                        {
                            message.IsAbandoned = true;
                            _logger.LogCritical(
                                "Outbox-сообщение {MessageId} исчерпало {MaxAttempts} попыток и помечено как заброшенное. Требуется ручное вмешательство.",
                                message.Id, _settings.MaxAttempts);
                        }
                        else
                        {
                            _logger.LogWarning(ex, "Не удалось опубликовать outbox-сообщение {MessageId} типа {MessageType}. " +
                                "Будет повторено на следующем опросе.",
                                message.Id.ToString(), message.MessageType);
                        }
                    }
                }

                await _outboxRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await _unitOfWork.CommitTransactionAsync().ConfigureAwait(false);

                return publishedCount;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
