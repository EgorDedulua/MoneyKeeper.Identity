using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Core.Entities;
using MoneyKeeper.Identity.Infrastructure.Data.Configurations;

namespace MoneyKeeper.Identity.Infrastructure.Data
{
    public class IdentityDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UsersConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokensConfiguration());
        }
    }
}
