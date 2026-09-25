using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities.Platform;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="PlatformAdmin"/> -&gt; dbo.PlatformAdmins, in the Platform
    /// database only. Applied explicitly by <see cref="PlatformDbContext"/>, same as <see cref="TenantConfiguration"/>.
    /// </summary>
    public class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
    {
        public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
        {
            builder.ToTable("PlatformAdmins");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");

            builder.Property(a => a.Email).IsRequired().HasMaxLength(256);
            builder.HasIndex(a => a.Email).IsUnique();

            builder.Property(a => a.PasswordHash).IsRequired().HasMaxLength(512);

            builder.Property(a => a.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.LastName).HasMaxLength(100);

            builder.Property(a => a.IsActive).IsRequired();
            builder.Property(a => a.CreatedAtUtc).IsRequired();

            builder.Ignore(a => a.FullName);
        }
    }
}
