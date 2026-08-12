using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Core.Entities;
using MoneyKeeper.Identity.Infrastructure.Data.Repositories;
using MoneyKeeper.Identity.IntegrationTests.Common;

namespace MoneyKeeper.Identity.IntegrationTests
{
    public class UsersRepositoryTests : RepositoryTestsBase
    {
        private readonly UsersRepository _usersRepository = null!;

        public UsersRepositoryTests(DbFixture fixture) : base(fixture)
        {
            _usersRepository = new UsersRepository(_context);
        }

        private User User => new User
        {
            Email = "email",
            Password = "password",
            UserName = "Name"
        };

        [Fact] 
        public async Task Test_AddUser_SuccesfullyAddsUser()
        {
            (await _context.Users.CountAsync()).Should().Be(0);

            await _usersRepository.AddAsync(User, CancellationToken.None);

            (await _context.Users.CountAsync()).Should().Be(1);
            User addedUser = await _context.Users.FirstAsync();
            addedUser.Id.Should().Be(1);
            addedUser.Email.Should().Be(User.Email);
            addedUser.Password.Should().Be(User.Password);
            addedUser.UserName.Should().Be(User.UserName);
        }

        [Fact]
        public async Task Test_GetByEmail_ReturnsUser()
        {
            await _usersRepository.AddAsync(User, CancellationToken.None);

            User? userFromDb = await _usersRepository.GetByEmailAsync(User.Email, CancellationToken.None);
            userFromDb.Should().NotBeNull();
            userFromDb.Id.Should().Be(1);
            userFromDb.Email.Should().Be(User.Email);
            userFromDb.Password.Should().Be(User.Password);
            userFromDb.UserName.Should().Be(User.UserName);
        }

        [Fact]
        public async Task Test_GetByEmail_ReturnsNull()
        {
            await _usersRepository.AddAsync(User, CancellationToken.None);

            User? userFromDb = await _usersRepository.GetByEmailAsync("nonexisten", CancellationToken.None);
            userFromDb.Should().BeNull();
        }

        [Fact]
        public async Task Test_GetById_ReturnsUser()
        {
            await _usersRepository.AddAsync(User, CancellationToken.None);

            User? userFromDb = await _usersRepository.GetByIdAsync(1, CancellationToken.None);
            userFromDb.Should().NotBeNull();
            userFromDb.Id.Should().Be(1);
            userFromDb.Email.Should().Be(User.Email);
            userFromDb.Password.Should().Be(User.Password);
            userFromDb.UserName.Should().Be(User.UserName);
        }

        [Fact]
        public async Task Test_GetById_ReturnsNull()
        {
            await _usersRepository.AddAsync(User, CancellationToken.None);

            User? userFromDb = await _usersRepository.GetByIdAsync(2, CancellationToken.None);
            userFromDb.Should().BeNull();
        }
    }
}
