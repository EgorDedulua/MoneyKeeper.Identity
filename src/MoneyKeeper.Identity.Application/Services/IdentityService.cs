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
        private readonly IIdentityRepository _identityRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;

        public IdentityService(IIdentityRepository identityRepository, IPasswordHasher passwordHasher, IJwtService jwtService)
        {
            _identityRepository = identityRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<Result<AuthResult>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            User? user = await _identityRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user is null || !_passwordHasher.Verify(user.Password, request.Password))
            {
                return Result<AuthResult>.Failure
                    (Error.Unauthorized("Неверная почта или пароль", ErrorCodes.INVALID_EMAIL_OR_PASSWORD));
            }
            string token = _jwtService.Generate(user);
            AuthResult response = new AuthResult(user.Email, user.UserName, user.Id, user.CreatedAt, token);

            return Result<AuthResult>.Success(response);
        }

        public async Task<Result<AuthResult>> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            if (await _identityRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
            {
                return Result<AuthResult>.Failure
                    (Error.Conflict("Почта уже занята", ErrorCodes.EMAIL_ALREADY_TAKEN));
            }

            User user = new User
            {
                Email = request.Email,
                UserName = request.UserName,
            };
            string passwordHash = _passwordHasher.Hash(request.Password);
            user.Password = passwordHash;
            await _identityRepository.AddAsync(user, cancellationToken);
            string token = _jwtService.Generate(user);
            AuthResult response = new AuthResult(user.Email, user.UserName, user.Id, user.CreatedAt, token);

            return Result<AuthResult>.Success(response);
        }
    }
}
