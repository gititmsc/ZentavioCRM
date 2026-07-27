using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");

            builder.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(30);
            builder.Property(a => a.Summary).IsRequired().HasMaxLength(1000);

            builder.Property(a => a.CreatedAtUtc).IsRequired();

            builder.HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(a => new { a.EntityType, a.EntityId });
        }
    }
}
