using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Core.Common.Interfaces
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

        Task<List<OutboxMessage>> GetPendingBatchForUpdateAsync(int batchSize, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<int> DeleteProcessedBatchAsync(DateTime processedBefore, int batchSize, CancellationToken cancellationToken = default);

        Task<List<OutboxMessage>> GetStaleUnprocessedAsync(DateTime staleBefore, int limit, CancellationToken cancellationToken = default);
    }
}
