using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Infrastructure.Data.Configurations
{
    public class OutboxMessagesConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.MessageType).HasMaxLength(200).IsRequired();

            builder.Property(m => m.RoutingKey).IsRequired();

            builder.Property(m => m.Payload).IsRequired();

            builder.Property(m => m.LastError).HasMaxLength(2000);

            builder.HasIndex(m => m.CreatedAt)
                .HasFilter("[ProcessedAt] IS NULL AND [IsAbandoned] = 0");

            builder.HasIndex(m => m.ProcessedAt)
                .HasFilter("[ProcessedAt] IS NOT NULL");
        }
    }
}
