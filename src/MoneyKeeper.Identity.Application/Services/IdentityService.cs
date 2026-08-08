using Microsoft.Extensions.Logging;
using MoneyKeeper.Identity.Application.Common;
using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Core.Common;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Application.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IRefreshTokensRepository _refreshTokensRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(IUsersRepository identityRepository, IRefreshTokensRepository refreshTokensRepository, 
            IPasswordHasher passwordHasher, IJwtService jwtService, ILogger<IdentityService> logger)
        {
            _usersRepository = identityRepository;
            _refreshTokensRepository = refreshTokensRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<Result<AuthResult>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            User? user = await _usersRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user is null || !_passwordHasher.Verify(user.Password, request.Password))
            {
                _logger.LogWarning("Неудачная попытка входа с почтой {Email}", request.Email);
                return Result<AuthResult>.Failure
                    (Error.Unauthorized("Неверная почта или пароль", ErrorCodes.INVALID_EMAIL_OR_PASSWORD));
            }
            string accessToken = _jwtService.GenerateAccessToken(user);
            string refreshToken = _jwtService.GenerateRefreshToken();

            RefreshToken refreshTokenEntity = new RefreshToken
            {
                TokenHash = _jwtService.ComputeHash(refreshToken),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);
            await _refreshTokensRepository.RevokeAllUserTokensAsync(user.Id, cancellationToken);
            AuthResult response = new AuthResult(user.Email, user.UserName, user.Id, user.CreatedAt, accessToken, refreshToken);
            _logger.LogInformation("Пользователь с id {UserId} успешно вошел в свой аккаунт", user.Id);

            return Result<AuthResult>.Success(response);
        }

        public async Task<Result<AuthResult>> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            if (await _usersRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
            {
                _logger.LogWarning("Попытка регистрации аккаунта с занятой почтой {Email}", request.Email);
                return Result<AuthResult>.Failure
                    (Error.Conflict("Почта уже занята", ErrorCodes.EMAIL_ALREADY_TAKEN));
            }

            User user = new User
            {
                Email = request.Email,
                UserName = request.UserName,
                Password = _passwordHasher.Hash(request.Password)
            };
            await _usersRepository.AddAsync(user, cancellationToken);
            string accessToken = _jwtService.GenerateAccessToken(user);
            string refreshToken = _jwtService.GenerateRefreshToken();
            RefreshToken refreshTokenEntity = new RefreshToken
            {
                TokenHash = _jwtService.ComputeHash(refreshToken),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);
            AuthResult response = new AuthResult(user.Email, user.UserName, user.Id, user.CreatedAt, accessToken, refreshToken);
            _logger.LogInformation("Завершена регистрация пользователя с id {UserId}", user.Id);

            return Result<AuthResult>.Success(response);
        }

        public async Task<Result<AccessTokenUpdateResponse>> Refresh(string? refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogDebug("Попытка обновления токена без предоставления refresh token - проигнорировано");
                return Result<AccessTokenUpdateResponse>.Failure
                    (Error.Unauthorized("Refresh token отсутствует", ErrorCodes.INVALID_TOKEN));
            }

            string hash = _jwtService.ComputeHash(refreshToken);
            RefreshToken? previousToken =
                await _refreshTokensRepository.GetByPreviousTokenHashAsync(hash, cancellationToken);
            if (previousToken is not null)
            {
                await _refreshTokensRepository.RevokeAllUserTokensAsync(previousToken.UserId, cancellationToken);
                _logger.LogWarning("Отозваны все refresh tokens пользователя с id {UserId}, так как обнаружено повторное использование токена"
                        , previousToken.UserId);
                return Result<AccessTokenUpdateResponse>.Failure
                    (Error.Unauthorized("Обнаружено повторное использование токена!", ErrorCodes.TOKEN_REUSE_DETECTED));
            }

            RefreshToken? storedToken =
                await _refreshTokensRepository.GetByHashAsync(hash, cancellationToken);
            if (storedToken is null || !storedToken.IsActive || storedToken.IsReplaced)
            {
                _logger.LogWarning("Попытка обновления с невалидным refresh token с хешем {TokenHash}", hash);
                return Result<AccessTokenUpdateResponse>.Failure
                    (Error.Unauthorized("Невалидный refresh token", ErrorCodes.INVALID_TOKEN));
            }

            User? user = await _usersRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogError("Не найден пользователь с id {UserId}, который предоставил верный refresh token", storedToken.UserId);
                return Result<AccessTokenUpdateResponse>.Failure
                    (Error.Unauthorized("Пользователь не найден", ErrorCodes.USER_NOT_FOUND));
            }

            string newAccessToken = _jwtService.GenerateAccessToken(user);
            string newRefreshToken = _jwtService.GenerateRefreshToken();
            string newHash = _jwtService.ComputeHash(newRefreshToken);
            await _refreshTokensRepository.RefreshToken(storedToken, newHash, cancellationToken);
            _logger.LogInformation("Обновлен refresh token для пользователя с id {UserId}", storedToken.UserId);

            return Result<AccessTokenUpdateResponse>.Success
                (new AccessTokenUpdateResponse(newAccessToken, newRefreshToken));
        }

        public async Task Logout(string? refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogDebug("Попытка выхода из аккаунта без предоставления refresh token - проигнорировано");
                return;
            }
            string hash = _jwtService.ComputeHash(refreshToken);
            RefreshToken? storedToken =
                await _refreshTokensRepository.GetByHashAsync(hash, cancellationToken);
            if (storedToken is not null && !storedToken.IsReplaced)
            {
                await _refreshTokensRepository.RevokeTokenAsync(storedToken, cancellationToken);
                _logger.LogInformation("Пользователь с id {UserId} вышел из своего аккаунта", storedToken.UserId);
            }
            _logger.LogDebug("Попытка выхода из аккаунта с невалидным токеноа - проигнорировано");
        }
    }
}
