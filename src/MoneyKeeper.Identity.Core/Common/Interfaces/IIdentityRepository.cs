using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Core.Common.Interfaces
{
    public interface IIdentityRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
