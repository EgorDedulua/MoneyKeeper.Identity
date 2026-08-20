using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Core.Common;

namespace MoneyKeeper.Identity.Application.Common.Interfaces.Auth
{
    public interface IIdentityService
    {
        Task<Result<AuthResult>> Register(RegisterRequest request, CancellationToken cancellationToken);

        Task<Result<AuthResult>> Login(LoginRequest request, CancellationToken cancellationToken);

        Task<Result<AccessTokenUpdateResponse>> Refresh(string? refreshToken, CancellationToken cancellationToken);

        Task Logout(string? refreshToken, CancellationToken cancellationToken);
    }
}
