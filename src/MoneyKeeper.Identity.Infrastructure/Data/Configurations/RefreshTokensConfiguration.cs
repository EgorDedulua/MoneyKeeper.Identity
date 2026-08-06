using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Infrastructure.Data.Configurations
{
    public class RefreshTokensConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder
                .HasKey(t => t.TokenHash);

            builder
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);

            builder
                .HasIndex(t => new { t.UserId, t.IsActive })
                .HasFilter("[RevokedAt] IS NOT NULL");

            builder
                .HasIndex(t => t.PreviousTokenHash)
                .IsUnique()
                .HasFilter("[PreviousTokenHash] IS NOT NULL");

            builder
                .Property(t => t.CreatedAt)
                .IsRequired()
                .HasDefaultValue(DateTime.UtcNow);

            builder
                .Property(t => t.ExpiresAt)
                .IsRequired();
        }
    }
}
