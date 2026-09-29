using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities.Platform;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="PlatformAuditLog"/> -&gt; dbo.PlatformAuditLogs, in the
    /// Platform database only. Applied explicitly by <see cref="PlatformDbContext"/>, same as
    /// <see cref="TenantConfiguration"/> and <see cref="PlatformAdminConfiguration"/>.
    /// </summary>
    public class PlatformAuditLogConfiguration : IEntityTypeConfiguration<PlatformAuditLog>
    {
        public void Configure(EntityTypeBuilder<PlatformAuditLog> builder)
        {
            builder.ToTable("PlatformAuditLogs");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");

            builder.Property(a => a.Action).IsRequired().HasMaxLength(30);
            builder.Property(a => a.Summary).IsRequired().HasMaxLength(1000);

            builder.Property(a => a.CreatedAtUtc).IsRequired();

            builder.HasOne(a => a.PlatformAdmin)
                .WithMany()
                .HasForeignKey(a => a.PlatformAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(a => a.TenantId);
            builder.HasIndex(a => a.CreatedAtUtc);
        }
    }
}
