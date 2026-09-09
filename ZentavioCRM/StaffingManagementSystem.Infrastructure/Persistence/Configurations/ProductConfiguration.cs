using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id).HasDefaultValueSql("NEWID()");

            builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);

            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);

            builder.Property(p => p.Type).IsRequired().HasConversion<int>();

            builder.Property(p => p.Category).HasMaxLength(100);

            builder.Property(p => p.Brand).HasMaxLength(100);

            builder.Property(p => p.UnitOfMeasure).HasMaxLength(50);

            builder.Property(p => p.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");

            builder.Property(p => p.Cost).HasColumnType("decimal(18,2)");

            builder.Property(p => p.TaxPercent).HasColumnType("decimal(5,2)");

            builder.Property(p => p.Description).HasMaxLength(2000);

            builder.Property(p => p.BillingType).HasMaxLength(50);

            builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(p => p.CreatedAtUtc).IsRequired();

            builder.HasIndex(p => p.Sku).IsUnique();

            builder.HasIndex(p => p.Name);
        }
    }
}
