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
            User user = User;

            await _usersRepository.AddAsync(user, CancellationToken.None);

            (await _context.Users.CountAsync()).Should().Be(1);
            User addedUser = await _context.Users.FirstAsync();
            addedUser.Id.Should().BePositive();
            addedUser.Email.Should().Be(user.Email);
            addedUser.Password.Should().Be(user.Password);
            addedUser.UserName.Should().Be(user.UserName);
        }

        [Fact]
        public async Task Test_GetByEmail_ReturnsUser()
        {
            User user = User;
            await _usersRepository.AddAsync(user, CancellationToken.None);

            User? userFromDb = await _usersRepository.GetByEmailAsync(user.Email, CancellationToken.None);
            userFromDb.Should().NotBeNull();
            userFromDb.Id.Should().BePositive();
            userFromDb.Email.Should().Be(user.Email);
            userFromDb.Password.Should().Be(user.Password);
            userFromDb.UserName.Should().Be(user.UserName);
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
            User user = User;
            await _usersRepository.AddAsync(user, CancellationToken.None);

            User? userFromDb = await _usersRepository.GetByIdAsync(user.Id, CancellationToken.None);
            userFromDb.Should().NotBeNull();
            userFromDb.Id.Should().Be(user.Id);
            userFromDb.Email.Should().Be(user.Email);
            userFromDb.Password.Should().Be(user.Password);
            userFromDb.UserName.Should().Be(user.UserName);
        }

        [Fact]
        public async Task Test_GetById_ReturnsNull()
        {
            User user = User;
            await _usersRepository.AddAsync(user, CancellationToken.None);

            User? userFromDb = await _usersRepository.GetByIdAsync(user.Id + 1, CancellationToken.None);
            userFromDb.Should().BeNull();
        }
    }
}
