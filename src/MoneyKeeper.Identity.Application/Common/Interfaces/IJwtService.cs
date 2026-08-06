using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);

        string GenerateRefreshToken();

        string ComputeHash(string rawData);
    }
}
