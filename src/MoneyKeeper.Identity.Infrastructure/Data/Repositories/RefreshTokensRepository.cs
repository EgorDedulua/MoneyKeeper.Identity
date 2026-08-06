using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Infrastructure.Data.Repositories
{
    public class RefreshTokensRepository : IRefreshTokensRepository
    {
        private readonly IdentityDbContext _db;

        public RefreshTokensRepository(IdentityDbContext db)
        {
            _db = db;
        }

        public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
        {
            await _db.RefreshTokens.AddAsync(token, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken cancellationToken)
        {
            return await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        }

        public async Task<RefreshToken?> GetByPreviousTokenHashAsync(string previousTokenHash, CancellationToken cancellationToken)
        {
            return await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.PreviousTokenHash == previousTokenHash, cancellationToken);
        }

        public async Task RefreshToken(RefreshToken token, string newHash, CancellationToken cancellationToken)
        {
            token.PreviousTokenHash = token.TokenHash;
            token.TokenHash = newHash;
            token.CreatedAt = DateTime.UtcNow;
            token.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken)
        {
            await _db.RefreshTokens
                .Where(t => t.UserId == userId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken);
        }

        public async Task RevokeTokenAsync(RefreshToken token, CancellationToken cancellationToken)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
