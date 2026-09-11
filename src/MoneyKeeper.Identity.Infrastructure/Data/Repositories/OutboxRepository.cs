using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Infrastructure.Data.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly IdentityDbContext _db;

        public OutboxRepository(IdentityDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            await _db.AddAsync(message, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> DeleteProcessedBatchAsync(DateTime processedBefore, int batchSize, CancellationToken cancellationToken = default)
        {
            var idsToDelete = await _db.OutboxMessages
                .Where(m => m.ProcessedAt != null && m.ProcessedAt < processedBefore)
                .Take(batchSize)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (idsToDelete.Count == 0)
                return 0;

            return await _db.OutboxMessages
                .Where(m => idsToDelete.Contains(m.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<OutboxMessage>> GetPendingBatchForUpdateAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var ids = await _db.Database
                .SqlQuery<Guid>($"""
                    SELECT TOP ({batchSize}) Id
                    FROM OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE ProcessedAt IS NULL AND IsAbandoned = 0
                    ORDER BY CreatedAt
                """)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (ids.Count == 0)
                return new List<OutboxMessage>();

            return await _db.OutboxMessages
                .Where(m => ids.Contains(m.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<OutboxMessage>> GetStaleUnprocessedAsync(DateTime staleBefore, int limit, CancellationToken cancellationToken = default)
        {
            return await _db.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.CreatedAt < staleBefore)
                .OrderBy(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
