using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Core.Common.Interfaces
{
    public interface IRefreshTokensRepository
    {
        Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);

        Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken cancellationToken);

        Task<RefreshToken?> GetByPreviousTokenHashAsync(string previousTokenHash, CancellationToken cancellationToken);

        Task RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken);

        Task RevokeTokenAsync(RefreshToken token, CancellationToken cancellationToken);

        Task RefreshToken(RefreshToken token, string newHash, CancellationToken cancellationToken);
    }
}
