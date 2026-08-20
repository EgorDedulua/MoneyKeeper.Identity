using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyKeeper.Identity.Application.Common;
using MoneyKeeper.Identity.Application.Common.Interfaces.Auth;
using MoneyKeeper.Identity.Application.Common.Interfaces.Messaging;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Application.Profiles;
using MoneyKeeper.Identity.Application.Services;
using MoneyKeeper.Identity.Core.Common;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;
using MoneyKeeper.Identity.Profiles;
using Moq;

namespace MoneyKeeper.Identity.UnitTests
{
    public class IdentityServiceTests
    {
        private readonly Mock<IUsersRepository> _usersRepositoryMock = new();
        private readonly Mock<IRefreshTokensRepository> _refreshTokensRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<ILogger<IdentityService>> _loggerMock = new();
        private readonly Mock<IMessageBus> _messgaeBusMock = new();
        private readonly IMapper _mapper;
        private const string HASH = "hash";
        private const string ACCESS_TOKEN = "access_token";
        private const string REFRESH_TOKEN = "refresh_token";
        private static readonly LoginRequest LoginRequst = new("Email", "Password");
        private static readonly RegisterRequest RegisterRequest = new(LoginRequst.Email, "Name", LoginRequst.Password);
        private static readonly User User = new User
        {
            Id = 1,
            Password = HASH,
            Email = RegisterRequest.Email,
            UserName = RegisterRequest.UserName
        };
        private static RefreshToken GetRefreshToken(RefreshTokenType tokenType) => new RefreshToken
        {
            Id = 1,
            UserId = 1,
            TokenHash = HASH,
            ExpiresAt = DateTime.UtcNow.AddDays(tokenType == RefreshTokenType.Expired ? -5 : 5),
            RevokedAt = tokenType == RefreshTokenType.Revoked ? DateTime.UtcNow.AddDays(-5) : null
        };

        public IdentityServiceTests()
        {
            _usersRepositoryMock.Reset();
            _refreshTokensRepositoryMock.Reset();
            _passwordHasherMock.Reset();
            _jwtServiceMock.Reset();
            _loggerMock.Reset();
            MapperConfiguration config = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<AuthResponseProfile>();
                    cfg.AddProfile<UserRegisteredEventProfile>();
                },
                new NullLoggerFactory()
            );

            _mapper = config.CreateMapper();
        }

        private IdentityService CreateService()
        {
            return new IdentityService(
                _usersRepositoryMock.Object,
                _refreshTokensRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtServiceMock.Object,
                _loggerMock.Object,
                _messgaeBusMock.Object,
                _mapper
            );
        }

        [Fact]
        public async Task Register_WhenEverythingIsCorrect_RegistersUser()
        {
            RegisterRequest request = RegisterRequest;
            _usersRepositoryMock.SetupGetByEmail(request.Email, null);
            _passwordHasherMock.Setup(p => p.Hash(request.Password)).Returns(HASH);
            _jwtServiceMock.SetupComputeHash(HASH);
            _jwtServiceMock.SetupGenerateAccessToken(ACCESS_TOKEN);
            _jwtServiceMock.SetupGenerateRefreshToken(REFRESH_TOKEN);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Register(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.UserName.Should().Be(request.UserName);
            result.Value.AccessToken.Should().Be(ACCESS_TOKEN);
            result.Value.RefreshToken.Should().Be(REFRESH_TOKEN);
            result.Value.Email.Should().Be(request.Email);
            _passwordHasherMock
                .Verify(h => h.Hash(request.Password), Times.Once);
            _usersRepositoryMock
                .Verify(r => r.AddAsync(It.Is<User>(u =>
                    u.Password == HASH &&
                    u.Email == request.Email &&
                    u.UserName == request.UserName
                ), It.IsAny<CancellationToken>()), Times.Once());
            _jwtServiceMock
                .Verify(j => j.GenerateAccessToken(It.Is<User>(u =>
                    u.Password == HASH &&
                    u.Email == request.Email &&
                    u.UserName == request.UserName
                )), Times.Once());
            _refreshTokensRepositoryMock
                .Verify(m => m.AddRefreshTokenAsync(It.Is<RefreshToken>(t =>
                        t.TokenHash == HASH
                    ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Register_WhenUserEmailIsTaken_ReturnsConflictError()
        {
            RegisterRequest request = RegisterRequest;
            _usersRepositoryMock.SetupGetByEmail(request.Email, User);
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
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Login_WhenEverythingIsCorrect_LoginsUser()
        {
            LoginRequest request = LoginRequst;
            _usersRepositoryMock.SetupGetByEmail(request.Email, User);
            _passwordHasherMock.SetupVerifyPassword(HASH, request.Password, true);
            _jwtServiceMock.SetupComputeHash(HASH);
            _jwtServiceMock.SetupGenerateAccessToken(ACCESS_TOKEN);
            _jwtServiceMock.SetupGenerateRefreshToken(REFRESH_TOKEN);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Login(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.UserName.Should().Be(User.UserName);
            result.Value.AccessToken.Should().Be(ACCESS_TOKEN);
            result.Value.RefreshToken.Should().Be(REFRESH_TOKEN);
            result.Value.Email.Should().Be(request.Email);
            _passwordHasherMock
                .Verify(m => m.Verify(User.Password, request.Password), Times.Once);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(User), Times.Once);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Once);
            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeAllUserTokensAsync(User.Id, It.IsAny<CancellationToken>()), Times.Once);
            _refreshTokensRepositoryMock
                .Verify(m => m.AddRefreshTokenAsync(It.Is<RefreshToken>(t => 
                        t.UserId == User.Id && 
                        t.TokenHash == HASH
                    ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Login_WhenUserExistsButPasswordIsIncorrect_ReturnsUnauthorizedError()
        {
            LoginRequest requset = LoginRequst;
            _usersRepositoryMock.SetupGetByEmail(requset.Email, User);
            _passwordHasherMock.SetupVerifyPassword(HASH, requset.Password, false);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Login(requset, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_EMAIL_OR_PASSWORD);
            _passwordHasherMock
                .Verify(m => m.Verify(HASH, requset.Password), Times.Once);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeAllUserTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Login_WhenUserDoesNotExist_ReturnsUnauthorizedError()
        {
            LoginRequest request = LoginRequst;
            _usersRepositoryMock.SetupGetByEmail(request.Email, null);
            IdentityService service = CreateService();

            Result<AuthResult> result = await service.Login(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_EMAIL_OR_PASSWORD);
            _passwordHasherMock
                .Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeAllUserTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Refresh_WhenTokenIsValid_RefreshesToken()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByPreviousTokenHashAsync(null);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(GetRefreshToken(RefreshTokenType.Valid));
            _usersRepositoryMock.SetupGetById(User.Id, User);
            _jwtServiceMock.SetupGenerateAccessToken(ACCESS_TOKEN);
            _jwtServiceMock.SetupGenerateRefreshToken(REFRESH_TOKEN);
            _jwtServiceMock.SetupComputeHash(HASH);
            IdentityService service = CreateService();

            Result<AccessTokenUpdateResponse> result = await service.Refresh(REFRESH_TOKEN, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.AccessToken.Should().Be(ACCESS_TOKEN);
            result.Value.RefreshToken.Should().Be(REFRESH_TOKEN);
            _refreshTokensRepositoryMock
                .Verify(m => m.RefreshTokenAsync(1, HASH, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Refresh_WhenInputTokenIsEmpty_ReturnsUnauthorizedError()
        {
            IdentityService service = CreateService();

            Result<AccessTokenUpdateResponse> result = await service.Refresh(null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_TOKEN);
            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeAllUserTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.RefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Refresh_WhenReuseIsDetected_ReturnsUnauthorizedError()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            _refreshTokensRepositoryMock.SetupGetByPreviousTokenHashAsync(refreshToken);
            IdentityService service = CreateService();

            Result<AccessTokenUpdateResponse> result = await service.Refresh(REFRESH_TOKEN, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.TOKEN_REUSE_DETECTED);
            _refreshTokensRepositoryMock
               .Verify(m => m.RevokeAllUserTokensAsync(refreshToken.UserId, It.IsAny<CancellationToken>()), Times.Once);
            _refreshTokensRepositoryMock
                .Verify(m => m.RefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Refresh_WhenTokenIsNotFound_ReturnsUnauthorizedError()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByPreviousTokenHashAsync(null);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(null);
            IdentityService service = CreateService();

            Result<AccessTokenUpdateResponse> result = await service.Refresh(REFRESH_TOKEN, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_TOKEN);
            _refreshTokensRepositoryMock
               .Verify(m => m.RevokeAllUserTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.RefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Refresh_WhenTokenIsRevoked_ReturnsUnauthorizedError()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByPreviousTokenHashAsync(null);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(GetRefreshToken(RefreshTokenType.Revoked));
            IdentityService service = CreateService();

            Result<AccessTokenUpdateResponse> result = await service.Refresh(REFRESH_TOKEN, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_TOKEN);
            _refreshTokensRepositoryMock
               .Verify(m => m.RevokeAllUserTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.RefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Refresh_WhenTokenIsExpired_ReturnsUnauthorizedError()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByPreviousTokenHashAsync(null);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(GetRefreshToken(RefreshTokenType.Expired));
            IdentityService service = CreateService();

            Result<AccessTokenUpdateResponse> result = await service.Refresh(REFRESH_TOKEN, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_TOKEN);
            _refreshTokensRepositoryMock
               .Verify(m => m.RevokeAllUserTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.RefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Refresh_WhenTokenIsValidButUserIsNotFound_ReturnsUnauthorizedError()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByPreviousTokenHashAsync(null);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(GetRefreshToken(RefreshTokenType.Valid));
            _usersRepositoryMock.SetupGetById(User.Id, null);
            IdentityService service = CreateService();

            Result<AccessTokenUpdateResponse> result = await service.Refresh(REFRESH_TOKEN, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_NOT_FOUND);
            _refreshTokensRepositoryMock
               .Verify(m => m.RevokeAllUserTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _refreshTokensRepositoryMock
                .Verify(m => m.RefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _jwtServiceMock
                .Verify(m => m.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Logout_WhenTokenIsValid_LogsUserOut()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(refreshToken);
            IdentityService service = CreateService();

            await service.Logout(REFRESH_TOKEN, CancellationToken.None);

            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeTokenAsync(refreshToken.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Logout_WhenTokenIsEmpty_LoggingOutIgnores()
        {
            IdentityService service = CreateService();

            await service.Logout(null, CancellationToken.None);

            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeTokenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Logout_WhenTokenIsNotFound_LoggingOutIgnores()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(null);
            IdentityService service = CreateService();

            await service.Logout(REFRESH_TOKEN, CancellationToken.None);

            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeTokenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Logout_WhenTokenIsRevoked_LoggingOutIgnores()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(GetRefreshToken(RefreshTokenType.Revoked));
            IdentityService service = CreateService();

            await service.Logout(REFRESH_TOKEN, CancellationToken.None);

            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeTokenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Logout_WhenTokenIsExpired_LoggingOutIgnores()
        {
            _jwtServiceMock.SetupComputeHash(HASH);
            _refreshTokensRepositoryMock.SetupGetByHashAsync(GetRefreshToken(RefreshTokenType.Expired));
            IdentityService service = CreateService();

            await service.Logout(REFRESH_TOKEN, CancellationToken.None);

            _refreshTokensRepositoryMock
                .Verify(m => m.RevokeTokenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
