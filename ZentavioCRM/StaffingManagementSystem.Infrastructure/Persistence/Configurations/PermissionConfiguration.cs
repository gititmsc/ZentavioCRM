using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => p.Code).IsUnique();

            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Module).IsRequired().HasMaxLength(100);
        }
    }
}
