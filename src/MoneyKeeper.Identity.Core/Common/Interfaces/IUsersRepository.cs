using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Core.Common.Interfaces
{
    public interface IUsersRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken);

        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);
    }
}
