using FluentAssertions;
using MoneyKeeper.Identity.Application.Common;
using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Application.Services;
using MoneyKeeper.Identity.Core.Common;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;
using Moq;

namespace MoneyKeeper.Identity.Application.Tests
{
    public class IdentityServiceTests
    {
        private readonly Mock<IUsersRepository> _usersRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();

        private IdentityService CreateService()
        {
            return new IdentityService(
                _usersRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtServiceMock.Object
            );
        }

        [Fact]
        public async Task Register_WhenEverythingIsCorrect_RegistersUser()
        {
            RegisterRequest request = IdentityServiceMockHelper.RegisterRequest;
            _usersRepositoryMock.SetupGetByEmail(request.Email, null);
            string token = "token";
            string hash = "hash";
            _passwordHasherMock.Setup(p => p.Hash(request.Password)).Returns(hash);
            _jwtServiceMock.SetupGenerate(token);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Register(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.UserName.Should().Be(request.UserName);
            result.Value.Token.Should().Be(token);
            result.Value.Email.Should().Be(request.Email);
            _passwordHasherMock
                .Verify(h => h.Hash(request.Password), Times.Once);
            _usersRepositoryMock
                .Verify(r => r.AddAsync(It.Is<User>(u =>
                    u.Password == hash &&
                    u.Email == request.Email &&
                    u.UserName == request.UserName
                ), It.IsAny<CancellationToken>()), Times.Once());
            _jwtServiceMock
                .Verify(j => j.Generate(It.Is<User>(u =>
                    u.Password == hash &&
                    u.Email == request.Email &&
                    u.UserName == request.UserName
                )), Times.Once());
        }

        [Fact]
        public async Task Register_WhenUserEmailIsTaken_ReturnsConflictError()
        {
            RegisterRequest request = IdentityServiceMockHelper.RegisterRequest;
            _usersRepositoryMock.SetupGetByEmail(request.Email, new User
            {
                Email = request.Email,
                Password = request.Password,
                UserName = request.UserName
            });
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Register(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.EMAIL_ALREADY_TAKEN);
            _passwordHasherMock
                .Verify(m => m.Hash(It.IsAny<string>()), Times.Never);
            _usersRepositoryMock
                .Verify(m => m.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.Generate(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Login_WhenEverythingIsCorrect_LoginsUser()
        {
            LoginRequest request = IdentityServiceMockHelper.LoginRequst;
            string hash = "hash";
            string token = "token";
            User user = new User
            {
                Email = request.Email,
                Password = hash,
                UserName = "Name"
            };
            _usersRepositoryMock.SetupGetByEmail(request.Email, user);
            _passwordHasherMock.SetupVerifyPassword(hash, request.Password, true);
            _jwtServiceMock.SetupGenerate(token);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Login(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.UserName.Should().Be(user.UserName);
            result.Value.Token.Should().Be(token);
            result.Value.Email.Should().Be(request.Email);
            _passwordHasherMock
                .Verify(m => m.Verify(user.Password, request.Password), Times.Once);
            _jwtServiceMock
                .Verify(m => m.Generate(user), Times.Once);
        }

        [Fact]
        public async Task Login_WhenUserExistsButPasswordIsIncorrect_ReturnsUnauthorizedError()
        {
            LoginRequest requset = IdentityServiceMockHelper.LoginRequst;
            string hash = "hash";
            User user = new User
            {
                Password = hash,
                UserName = "Name",
                Email = requset.Email
            };
            _usersRepositoryMock.SetupGetByEmail(requset.Email, user);
            _passwordHasherMock.SetupVerifyPassword(hash, requset.Password, false);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Login(requset, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_EMAIL_OR_PASSWORD);
            _passwordHasherMock
                .Verify(m => m.Verify(hash, requset.Password), Times.Once);
            _jwtServiceMock
                .Verify(m => m.Generate(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Login_WhenUserDoesNotExist_ReturnsUnauthorizedError()
        {
            LoginRequest request = IdentityServiceMockHelper.LoginRequst;
            _usersRepositoryMock.SetupGetByEmail(request.Email, null);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Login(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_EMAIL_OR_PASSWORD);
            _passwordHasherMock
                .Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
