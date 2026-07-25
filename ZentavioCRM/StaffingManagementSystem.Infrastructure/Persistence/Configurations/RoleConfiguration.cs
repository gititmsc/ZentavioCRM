using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id).HasDefaultValueSql("NEWID()");

            builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
            builder.HasIndex(r => r.Name).IsUnique();

            builder.Property(r => r.Description).HasMaxLength(500);

            builder.Property(r => r.CreatedAtUtc).IsRequired();
        }
    }
}
