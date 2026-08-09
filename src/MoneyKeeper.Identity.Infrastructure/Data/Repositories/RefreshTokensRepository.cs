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
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        }

        public async Task<RefreshToken?> GetByPreviousTokenHashAsync(string previousTokenHash, CancellationToken cancellationToken)
        {
            return await _db.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PreviousTokenHash == previousTokenHash, cancellationToken);
        }

        public async Task RefreshToken(int tokenId, string newHash, CancellationToken cancellationToken)
        {
            await _db.RefreshTokens
                .Where(t => t.Id == tokenId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.PreviousTokenHash, t => t.TokenHash)
                    .SetProperty(t => t.TokenHash, newHash)
                    .SetProperty(t => t.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(t => t.ExpiresAt, DateTime.UtcNow.AddDays(7)), cancellationToken);
        }

        public async Task RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken)
        {
            await _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken);
        }

        public async Task RevokeTokenAsync(int tokenId, CancellationToken cancellationToken)
        {
            await _db.RefreshTokens
                .Where(t => t.Id == tokenId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken);
        }
    }
}
