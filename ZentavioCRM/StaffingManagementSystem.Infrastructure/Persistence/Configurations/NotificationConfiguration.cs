using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id).HasDefaultValueSql("NEWID()");

            builder.Property(n => n.Message).IsRequired().HasMaxLength(500);
            builder.Property(n => n.RelatedEntityType).HasConversion<string>().HasMaxLength(30);

            builder.Property(n => n.CreatedAtUtc).IsRequired();

            builder.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });
        }
    }
}
