using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tags");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasDefaultValueSql("NEWID()");

            builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
            builder.HasIndex(t => t.Name).IsUnique();

            builder.Property(t => t.Color).HasMaxLength(20);

            builder.Property(t => t.CreatedAtUtc).IsRequired();
        }
    }
}
