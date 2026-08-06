using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Core.Common;

namespace MoneyKeeper.Identity.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<Result<AuthResult>> Register(RegisterRequest request, CancellationToken cancellationToken = default);

        Task<Result<AuthResult>> Login(LoginRequest request, CancellationToken cancellationToken = default);
    }
}
