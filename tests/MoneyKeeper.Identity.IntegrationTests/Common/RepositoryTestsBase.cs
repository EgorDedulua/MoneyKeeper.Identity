using Microsoft.EntityFrameworkCore.Storage;
using MoneyKeeper.Identity.Infrastructure.Data;

namespace MoneyKeeper.Identity.IntegrationTests.Common
{
    public abstract class RepositoryTestsBase : IClassFixture<DbFixture>, IAsyncLifetime
    {
        protected readonly IdentityDbContext _context;
        private IDbContextTransaction _transaction = null!;

        protected RepositoryTestsBase(DbFixture fixture)
        {
            _context = new IdentityDbContext(fixture.DbOptions);
        }

        public async Task InitializeAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task DisposeAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }

            if (_context is not null)
                await _context.DisposeAsync();
        }
    }
}
