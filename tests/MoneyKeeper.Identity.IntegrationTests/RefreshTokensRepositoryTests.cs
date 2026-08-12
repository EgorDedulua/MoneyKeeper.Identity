using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Core.Entities;
using MoneyKeeper.Identity.Infrastructure.Data.Repositories;
using MoneyKeeper.Identity.IntegrationTests.Common;
using MoneyKeeper.Identity.UnitTests;

namespace MoneyKeeper.Identity.IntegrationTests
{
    public class RefreshTokensRepositoryTests : RepositoryTestsBase
    {
        private readonly RefreshTokensRepository _refreshTokensRepository = null!;

        public RefreshTokensRepositoryTests(DbFixture fixture) : base(fixture)
        {
            _refreshTokensRepository = new RefreshTokensRepository(_context);
        }

        private int _tokenCounter = 0;
        private RefreshToken GetRefreshToken(RefreshTokenType tokenType)
        {
            ++_tokenCounter;
            return new RefreshToken
            {
                TokenHash = $"hash_{_tokenCounter}",
                PreviousTokenHash = $"prevHash_{_tokenCounter}",
                ExpiresAt = DateTime.UtcNow.AddDays(tokenType == RefreshTokenType.Expired ? -5 : 5),
                RevokedAt = tokenType == RefreshTokenType.Revoked ? DateTime.UtcNow.AddDays(-5) : null
            };
        }

        private async Task<User> CreateUserAsync(string email)
        {
            User user = new User
            {
                Email = email,
                Password = "password",
                UserName = "Name"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user; 
        }

        [Fact]
        public async Task Test_AddRefreshToken_SuccesfullyAddsRefreshToken()
        {
            (await _context.RefreshTokens.CountAsync()).Should().Be(0);
            User user = await CreateUserAsync("email@gmail.com");
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            refreshToken.UserId = user.Id;

            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            (await _context.RefreshTokens.CountAsync()).Should().Be(1);
            RefreshToken addedRefreshToken = await _context.RefreshTokens.FirstAsync();
            addedRefreshToken.Id.Should().BePositive();
            addedRefreshToken.CreatedAt.Should().Be(refreshToken.CreatedAt);
            addedRefreshToken.UpdatedAt.Should().Be(refreshToken.UpdatedAt);
            addedRefreshToken.ExpiresAt.Should().Be(refreshToken.ExpiresAt);
            addedRefreshToken.RevokedAt.Should().Be(refreshToken.RevokedAt);
            addedRefreshToken.PreviousTokenHash.Should().Be(refreshToken.PreviousTokenHash);
            addedRefreshToken.TokenHash.Should().Be(refreshToken.TokenHash);
        }

        [Fact]
        public async Task Test_GetByHash_ReturnsRefreshToken()
        {
            User user = await CreateUserAsync("email@gmail.com");
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            refreshToken.UserId = user.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);
            
            RefreshToken? fromDb = await _refreshTokensRepository.GetByHashAsync(refreshToken.TokenHash, CancellationToken.None);

            fromDb.Should().NotBeNull();
            fromDb.CreatedAt.Should().Be(refreshToken.CreatedAt);
            fromDb.UpdatedAt.Should().Be(refreshToken.UpdatedAt);
            fromDb.ExpiresAt.Should().Be(refreshToken.ExpiresAt);
            fromDb.RevokedAt.Should().Be(refreshToken.RevokedAt);
            fromDb.PreviousTokenHash.Should().Be(refreshToken.PreviousTokenHash);
            fromDb.TokenHash.Should().Be(refreshToken.TokenHash);
        }

        [Fact]
        public async Task Test_GetByHash_ReturnsNull()
        {
            User user = await CreateUserAsync("email@gmail.com");
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            refreshToken.UserId = user.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            RefreshToken? fromDb = await _refreshTokensRepository.GetByHashAsync("nonexisten", CancellationToken.None);

            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task Test_GetByPreviousTokenHash_ReturnsRefreshToken()
        {
            User user = await CreateUserAsync("email@gmail.com");
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            refreshToken.UserId = user.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            RefreshToken? fromDb = await _refreshTokensRepository.GetByPreviousTokenHashAsync(refreshToken.PreviousTokenHash!, CancellationToken.None);
            
            fromDb.Should().NotBeNull();
            fromDb.CreatedAt.Should().Be(refreshToken.CreatedAt);
            fromDb.UpdatedAt.Should().Be(refreshToken.UpdatedAt);
            fromDb.ExpiresAt.Should().Be(refreshToken.ExpiresAt);
            fromDb.RevokedAt.Should().Be(refreshToken.RevokedAt);
            fromDb.PreviousTokenHash.Should().Be(refreshToken.PreviousTokenHash);
            fromDb.TokenHash.Should().Be(refreshToken.TokenHash);
        }

        [Fact]
        public async Task Test_GetByPreviousTokenHash_ReturnsNull()
        {
            User user = await CreateUserAsync("email@gmail.com");
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            refreshToken.UserId = user.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            RefreshToken? fromDb = await _refreshTokensRepository.GetByPreviousTokenHashAsync("nonexisten", CancellationToken.None);
            
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task Test_RefreshToken_RefreshesToken()
        {
            User user = await CreateUserAsync("email@gmail.com");
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            refreshToken.UserId = user.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);
            string newHash = "newHash";
            string oldHash = refreshToken.TokenHash;
            DateTime beforeRefresh = DateTime.UtcNow;

            await _refreshTokensRepository.RefreshTokenAsync(refreshToken.Id, newHash, CancellationToken.None);

            RefreshToken fromDb = await _context.RefreshTokens.FirstAsync(t => t.Id == refreshToken.Id);
            fromDb.Should().NotBeNull();
            fromDb.CreatedAt.Should().Be(refreshToken.CreatedAt);
            fromDb.UpdatedAt.Should().BeCloseTo(beforeRefresh, TimeSpan.FromSeconds(10));
            fromDb.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
            fromDb.RevokedAt.Should().Be(refreshToken.RevokedAt);
            fromDb.PreviousTokenHash.Should().Be(oldHash);
            fromDb.TokenHash.Should().Be(newHash);
        }

        [Fact]
        public async Task Test_RevokeAllUserTokens_SuccessfullyRevokesAllTokens()
        {
            User firstUser = await CreateUserAsync("first@gmail.com");
            User secondUser = await CreateUserAsync("second@gmail.com");
            RefreshToken token1 = GetRefreshToken(RefreshTokenType.Valid);
            RefreshToken token2 = GetRefreshToken(RefreshTokenType.Valid);
            token1.UserId = firstUser.Id;
            token2.UserId = firstUser.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(token1, CancellationToken.None);
            await _refreshTokensRepository.AddRefreshTokenAsync(token2, CancellationToken.None);
            RefreshToken secondUserRefreshToken = GetRefreshToken(RefreshTokenType.Valid);
            secondUserRefreshToken.UserId = secondUser.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(secondUserRefreshToken, CancellationToken.None);

            await _refreshTokensRepository.RevokeAllUserTokensAsync(firstUser.Id, CancellationToken.None);

            List<RefreshToken> revokedTokens = await _context.RefreshTokens
                .AsNoTracking()
                .Where(t => t.UserId == firstUser.Id)
                .ToListAsync();
            revokedTokens.Should().HaveCount(2);
            revokedTokens.ForEach(t => t.UserId.Should().Be(firstUser.Id));
            revokedTokens.ForEach(t => t.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10)));
            RefreshToken revokedToken1 = revokedTokens.First(t => t.Id == token1.Id);
            revokedToken1.TokenHash.Should().Be(token1.TokenHash);
            revokedToken1.PreviousTokenHash.Should().Be(token1.PreviousTokenHash);
            RefreshToken revokedToken2 = revokedTokens.First(t => t.Id == token2.Id);
            revokedToken2.TokenHash.Should().Be(token2.TokenHash);
            revokedToken2.PreviousTokenHash.Should().Be(token2.PreviousTokenHash);
        }

        [Fact]
        public async Task Test_RevokeAllUserTokens_SuccessfullyRevokesAllUserTokensAndIgnoresAlreadyRevokedTokens()
        {
            User firstUser = await CreateUserAsync("first@gmail.com");
            User secondUser = await CreateUserAsync("second@gmail.com");
            RefreshToken validRefreshToken = GetRefreshToken(RefreshTokenType.Valid);
            validRefreshToken.UserId = firstUser.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(validRefreshToken, CancellationToken.None);
            RefreshToken revokedRefreshToken = GetRefreshToken(RefreshTokenType.Revoked);
            revokedRefreshToken.UserId = firstUser.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(revokedRefreshToken, CancellationToken.None);
            RefreshToken secondUserRefreshToken = GetRefreshToken(RefreshTokenType.Valid);
            secondUserRefreshToken.UserId = secondUser.Id;
            await _refreshTokensRepository.AddRefreshTokenAsync(secondUserRefreshToken, CancellationToken.None);

            await _refreshTokensRepository.RevokeAllUserTokensAsync(firstUser.Id, CancellationToken.None);

            List<RefreshToken> allUserTokens = await _context.RefreshTokens
                .AsNoTracking()
                .Where(t => t.UserId == firstUser.Id)
                .ToListAsync();

            RefreshToken revokedValid = allUserTokens.First(t => t.Id == validRefreshToken.Id);
            revokedValid.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            revokedValid.TokenHash.Should().Be(validRefreshToken.TokenHash);
            revokedValid.PreviousTokenHash.Should().Be(validRefreshToken.PreviousTokenHash);

            RefreshToken alreadyRevoked = allUserTokens.First(t => t.Id == revokedRefreshToken.Id);
            alreadyRevoked.RevokedAt.Should().Be(revokedRefreshToken.RevokedAt);
            alreadyRevoked.TokenHash.Should().Be(revokedRefreshToken.TokenHash);
            alreadyRevoked.PreviousTokenHash.Should().Be(revokedRefreshToken.PreviousTokenHash);

            RefreshToken secondUserToken = await _context.RefreshTokens
                .AsNoTracking()
                .FirstAsync(t => t.Id == secondUserRefreshToken.Id);
            secondUserToken.RevokedAt.Should().BeNull();
        }
    }
}
