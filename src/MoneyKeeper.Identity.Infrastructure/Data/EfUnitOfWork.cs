using Microsoft.EntityFrameworkCore.Storage;
using MoneyKeeper.Identity.Application.Common.Interfaces;

namespace MoneyKeeper.Identity.Infrastructure.Data
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly IdentityDbContext _context;
        private IDbContextTransaction? _transaction;

        public EfUnitOfWork(IdentityDbContext identityDbContext)
        {
            _context = identityDbContext;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction is not null)
                throw new InvalidOperationException("Транзакция уже начата для этого UnitOfWork");

            _transaction = await _context.Database
                .BeginTransactionAsync()
                .ConfigureAwait(false);
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction is null)
                throw new InvalidOperationException("Транзакция не была начата для этого UnitOfWork");

            try
            {
                await _transaction.CommitAsync().ConfigureAwait(false);
            }
            finally
            {
                await _transaction.DisposeAsync().ConfigureAwait(false);
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction is null)
                return;

            try
            {
                await _transaction.RollbackAsync().ConfigureAwait(false);
            }
            finally
            {
                await _transaction.DisposeAsync().ConfigureAwait(false);
                _transaction = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction is not null)
                await _transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
