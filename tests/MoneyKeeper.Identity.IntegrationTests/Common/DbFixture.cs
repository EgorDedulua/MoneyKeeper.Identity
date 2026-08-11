using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Infrastructure.Data;
using Testcontainers.MsSql;

namespace MoneyKeeper.Identity.IntegrationTests.Common
{
    public class DbFixture : IAsyncLifetime
    {
        static DbFixture()
        {
            string envFile = Path.Combine(AppContext.BaseDirectory, ".env.test");
            if (File.Exists(envFile))
                Env.Load(envFile);
        }

        public MsSqlContainer Container { get; }

        public DbContextOptions<IdentityDbContext> DbOptions { get; private set; } = null!;

        public DbFixture()
        {
            string password = Environment.GetEnvironmentVariable("TEST_DB_PASSWORD")
                ?? throw new InvalidOperationException("TEST_DB_PASSWORD not set");

            Container = new MsSqlBuilder("mcr.microsoft.com/azure-sql-edge:latest")
                .WithPassword(password)
                .WithAutoRemove(true)
                .WithCleanUp(true)
                .Build();
        }

        public async Task InitializeAsync()
        {
            await Container.StartAsync();
            string connectionString = $"{Container.GetConnectionString()};Database=IdentityTest;TrustServerCertificate=true;";
            DbOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            using var context = new IdentityDbContext(DbOptions);
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await Container.DisposeAsync();
        }
    }
}
