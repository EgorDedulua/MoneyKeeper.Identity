using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Infrastructure.Data.Repositories
{
    public class UsersRepository : IUsersRepository
    {
        private readonly IdentityDbContext _db;

        public UsersRepository(IdentityDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            user.CreatedAt = DateTime.UtcNow;
            await _db.Users.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return await _db.Users
                .FirstOrDefaultAsync(u =>  u.Email == email, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _db.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }
    }
}
