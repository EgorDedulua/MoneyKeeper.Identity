using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;
using Moq;

namespace MoneyKeeper.Identity.Application.Tests
{
    public static class IdentityServiceMockHelper
    {
        public static RegisterRequest RegisterRequest => new("Email", "Name", "Password");

        public static LoginRequest LoginRequst => new("Email", "Password");

        public static void SetupGetByEmail(this Mock<IUsersRepository> mock, string email, User? value)
        {
            mock.Setup(m => m.GetByEmailAsync(email, It.IsAny<CancellationToken>())).
                ReturnsAsync(value);
        }

        public static void SetupVerifyPassword(this Mock<IPasswordHasher> mock, string hash, string password, bool value)
        {
            mock.Setup(m => m.Verify(hash, password))
                .Returns(value);
        }

        public static void SetupGenerate(this Mock<IJwtService> mock, string token)
        {
            mock.Setup(m => m.Generate(It.IsAny<User>()))
                .Returns(token);
        }
    }
}
