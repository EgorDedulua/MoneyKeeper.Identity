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

        private RefreshToken GetRefreshToken(RefreshTokenType tokenType) => new RefreshToken
        {
            Id = 1,
            UserId = 1,
            TokenHash = "hash",
            PreviousTokenHash = "previousTokenHash",
            ExpiresAt = DateTime.UtcNow.AddDays(tokenType == RefreshTokenType.Expired ? -5 : 5),
            RevokedAt = tokenType == RefreshTokenType.Revoked ? DateTime.UtcNow.AddDays(-5) : null
        };

        private User User => new User
        {
            Email = "email",
            Password = "password",
            UserName = "Name"
        };

        [Fact]
        public async Task Test_AddRefreshToken_SuccesfullyAddsRefreshToken()
        {
            (await _context.RefreshTokens.CountAsync()).Should().Be(0);
            await _context.Users.AddAsync(User);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);

            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            (await _context.RefreshTokens.CountAsync()).Should().Be(1);
            RefreshToken addedRefreshToken = await _context.RefreshTokens.FirstAsync();
            addedRefreshToken.Id.Should().Be(1);
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
            await _context.Users.AddAsync(User);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);
            
            RefreshToken? fromDb = await _refreshTokensRepository.GetByHashAsync(refreshToken.TokenHash, CancellationToken.None);
            fromDb.Should().NotBeNull();
            fromDb.Id.Should().Be(1);
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
            await _context.Users.AddAsync(User);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            RefreshToken? fromDb = await _refreshTokensRepository.GetByHashAsync("nonexisten", CancellationToken.None);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task Test_GetByPreviousTokenHash_ReturnsRefreshToken()
        {
            await _context.Users.AddAsync(User);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            RefreshToken? fromDb = await _refreshTokensRepository.GetByPreviousTokenHashAsync(refreshToken.PreviousTokenHash!, CancellationToken.None);
            fromDb.Should().NotBeNull();
            fromDb.Id.Should().Be(refreshToken.Id);
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
            await _context.Users.AddAsync(User);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);

            RefreshToken? fromDb = await _refreshTokensRepository.GetByPreviousTokenHashAsync("nonexisten", CancellationToken.None);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task Test_RefreshToken_RefreshesToken()
        {
            await _context.Users.AddAsync(User);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);
            string newHash = "newHash";
            string oldHash = refreshToken.TokenHash;

            await _refreshTokensRepository.RefreshTokenAsync(refreshToken.Id, newHash, CancellationToken.None);

            RefreshToken fromDb = await _context.RefreshTokens.FirstAsync(t => t.Id == refreshToken.Id);
            fromDb.Should().NotBeNull();
            fromDb.Id.Should().Be(refreshToken.Id);
            fromDb.CreatedAt.Should().Be(refreshToken.CreatedAt);
            fromDb.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            fromDb.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(10));
            fromDb.RevokedAt.Should().Be(refreshToken.RevokedAt);
            fromDb.PreviousTokenHash.Should().Be(oldHash);
            fromDb.TokenHash.Should().Be(newHash);
        }

        [Fact]
        public async Task Test_RevokeAllUserTokens_SuccessfullyRevokesAllTokens()
        {
            await _context.Users.AddAsync(User);
            RefreshToken refreshToken = GetRefreshToken(RefreshTokenType.Valid);
            User secondUser = User;
            secondUser.Email = "second";
            await _context.Users.AddAsync(secondUser);
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);
            await _refreshTokensRepository.AddRefreshTokenAsync(refreshToken, CancellationToken.None);
            RefreshToken secondUserRefreshToken = GetRefreshToken(RefreshTokenType.Valid);
            secondUserRefreshToken.UserId = 2;
            await _refreshTokensRepository.AddRefreshTokenAsync(secondUserRefreshToken, CancellationToken.None);

            await _refreshTokensRepository.RevokeAllUserTokensAsync(1, CancellationToken.None);

            List<RefreshToken> revokedTokens = await _context.RefreshTokens
                .Where(t => t.UserId == 1)
                .ToListAsync();
            revokedTokens.Should().HaveCount(2);
            revokedTokens.ForEach(t => t.UserId.Should().Be(1));
            revokedTokens.ForEach(t => t.CreatedAt.Should().Be(refreshToken.CreatedAt));
            revokedTokens.ForEach(t => t.UpdatedAt.Should().Be(refreshToken.UpdatedAt));
            revokedTokens.ForEach(t => t.ExpiresAt.Should().Be(refreshToken.ExpiresAt));
            revokedTokens.ForEach(t => t.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10)));
            revokedTokens.ForEach(t => t.PreviousTokenHash.Should().Be(refreshToken.PreviousTokenHash));
            revokedTokens.ForEach(t => t.TokenHash.Should().Be(refreshToken.TokenHash));
        }

        [Fact]
        public async Task Test_RevokeAllUserTokens_SuccessfullyRevokesAllUserTokensAndIgnoresAlreadyRevokedTokens()
        {
            await _context.Users.AddAsync(User);
            RefreshToken validRefreshToken = GetRefreshToken(RefreshTokenType.Valid);
            User secondUser = User;
            secondUser.Email = "second";
            await _context.Users.AddAsync(secondUser);
            await _refreshTokensRepository.AddRefreshTokenAsync(validRefreshToken, CancellationToken.None);
            RefreshToken revokedRefreshToken = GetRefreshToken(RefreshTokenType.Revoked);
            await _refreshTokensRepository.AddRefreshTokenAsync(revokedRefreshToken, CancellationToken.None);
            RefreshToken secondUserRefreshToken = GetRefreshToken(RefreshTokenType.Valid);
            secondUserRefreshToken.UserId = 2;
            await _refreshTokensRepository.AddRefreshTokenAsync(secondUserRefreshToken, CancellationToken.None);

            await _refreshTokensRepository.RevokeAllUserTokensAsync(1, CancellationToken.None);

            List<RefreshToken> revokedTokens = await _context.RefreshTokens
                .Where(t => t.UserId == 1 && DateTime.UtcNow - t.RevokedAt >= TimeSpan.FromSeconds(0))
                .ToListAsync();
            revokedTokens.Should().HaveCount(1);
            revokedTokens.ForEach(t => t.UserId.Should().Be(1));
            revokedTokens.ForEach(t => t.CreatedAt.Should().Be(validRefreshToken.CreatedAt));
            revokedTokens.ForEach(t => t.UpdatedAt.Should().Be(validRefreshToken.UpdatedAt));
            revokedTokens.ForEach(t => t.ExpiresAt.Should().Be(validRefreshToken.ExpiresAt));
            revokedTokens.ForEach(t => t.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10)));
            revokedTokens.ForEach(t => t.PreviousTokenHash.Should().Be(validRefreshToken.PreviousTokenHash));
            revokedTokens.ForEach(t => t.TokenHash.Should().Be(validRefreshToken.TokenHash));
        }
    }
}
