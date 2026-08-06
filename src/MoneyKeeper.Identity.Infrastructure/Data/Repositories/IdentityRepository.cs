using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Infrastructure.Data.Repositories
{
    public class IdentityRepository : IIdentityRepository
    {
        private readonly IdentityDbContext _db;

        public IdentityRepository(IdentityDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            user.CreatedAt = DateTime.Now;
            await _db.Users.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return await _db.Users
                .FirstOrDefaultAsync(u =>  u.Email == email, cancellationToken);
        }
    }
}
