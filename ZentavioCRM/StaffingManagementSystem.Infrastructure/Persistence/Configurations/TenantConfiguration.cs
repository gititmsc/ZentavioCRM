using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities.Platform;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="Tenant"/> -&gt; dbo.Tenants, in the Platform database only.
    /// Applied explicitly by <see cref="PlatformDbContext"/> — see its class remarks for why this
    /// is NOT picked up via ApplyConfigurationsFromAssembly.
    /// </summary>
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasDefaultValueSql("NEWID()");

            builder.Property(t => t.Name).IsRequired().HasMaxLength(200);

            builder.Property(t => t.Subdomain).IsRequired().HasMaxLength(63);
            builder.HasIndex(t => t.Subdomain).IsUnique();

            builder.Property(t => t.DatabaseName).IsRequired().HasMaxLength(128);
            builder.HasIndex(t => t.DatabaseName).IsUnique();

            builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(t => t.AdminEmail).IsRequired().HasMaxLength(256);

            builder.Property(t => t.CreatedAtUtc).IsRequired();
        }
    }
}
