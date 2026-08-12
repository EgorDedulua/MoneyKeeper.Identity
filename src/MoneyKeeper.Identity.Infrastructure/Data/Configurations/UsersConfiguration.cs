using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Infrastructure.Data.Configurations
{
    public class UsersConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder
                .HasKey(u => u.Id);

            builder
                .Property(u => u.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            builder
                .Property(u => u.Email)
                .IsRequired();

            builder
                .HasIndex(u => u.Email)
                .IsUnique();

            builder
                .Property(u => u.Password)
                .IsRequired();

            builder
                .Property(u => u.UserName)
                .IsRequired();

            builder
                .Property(u => u.CreatedAt)
                .IsRequired()
                .HasDefaultValue(DateTime.UtcNow);
        }
    }
}
