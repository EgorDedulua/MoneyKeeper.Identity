using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;
using Moq;

namespace MoneyKeeper.Identity.UnitTests
{
    public static class IdentityServiceTestsMocksExtensions
    {
        public static void SetupGetByEmail(this Mock<IUsersRepository> mock, string email, User? value)
        {
            mock.Setup(m => m.GetByEmailAsync(email, It.IsAny<CancellationToken>())).
                ReturnsAsync(value);
        }

        public static void SetupGetById(this Mock<IUsersRepository> mock, int userId, User? value)
        {
            mock.Setup(m => m.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public static void SetupVerifyPassword(this Mock<IPasswordHasher> mock, string hash, string password, bool value)
        {
            mock.Setup(m => m.Verify(hash, password))
                .Returns(value);
        }

        public static void SetupGenerateAccessToken(this Mock<IJwtService> mock, string token)
        {
            mock.Setup(m => m.GenerateAccessToken(It.IsAny<User>()))
                .Returns(token);
        }

        public static void SetupGenerateRefreshToken(this Mock<IJwtService> mock, string refreshToken)
        {
            mock.Setup(m => m.GenerateRefreshToken())
                .Returns(refreshToken);
        }

        public static void SetupComputeHash(this Mock<IJwtService> mock, string hash)
        {
            mock.Setup(m => m.ComputeHash(It.IsAny<string>()))
                .Returns(hash);
        }

        public static void SetupGetByHashAsync(this Mock<IRefreshTokensRepository> mock, RefreshToken? refreshToken)
        {
            mock.Setup(m => m.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshToken);
        }

        public static void SetupGetByPreviousTokenHashAsync(this Mock<IRefreshTokensRepository> mock, RefreshToken? refreshToken)
        {
            mock.Setup(m => m.GetByPreviousTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshToken);
        }
    }
}
