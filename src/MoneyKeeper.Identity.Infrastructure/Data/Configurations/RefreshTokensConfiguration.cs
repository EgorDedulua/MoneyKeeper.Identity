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
                .HasKey(t => t.Id);

            builder
                .Property(t => t.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            builder
                .HasIndex(t => t.TokenHash)
                .IsUnique();

            builder
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);

            builder
                .HasIndex(t => t.PreviousTokenHash)
                .IsUnique()
                .HasFilter("[PreviousTokenHash] IS NOT NULL");

            builder
                .Property(t => t.CreatedAt)
                .IsRequired()
                .HasDefaultValue(DateTime.UtcNow);

            builder
                .Property(t => t.UpdatedAt)
                .IsRequired()
                .HasDefaultValue(DateTime.UtcNow);

            builder
                .Property(t => t.ExpiresAt)
                .IsRequired();

            builder
                .Ignore(t => t.IsActive);
        }
    }
}
