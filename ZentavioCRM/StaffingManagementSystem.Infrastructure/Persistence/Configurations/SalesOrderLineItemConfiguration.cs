using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class SalesOrderLineItemConfiguration : IEntityTypeConfiguration<SalesOrderLineItem>
    {
        public void Configure(EntityTypeBuilder<SalesOrderLineItem> builder)
        {
            builder.ToTable("SalesOrderLineItems");

            builder.HasKey(li => li.Id);

            builder.Property(li => li.Id).HasDefaultValueSql("NEWID()");

            builder.Property(li => li.ProductName).IsRequired().HasMaxLength(200);
            builder.Property(li => li.Quantity).HasColumnType("decimal(18,2)");
            builder.Property(li => li.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(li => li.DiscountPercent).HasColumnType("decimal(5,2)");
            builder.Property(li => li.TaxPercent).HasColumnType("decimal(5,2)");
            builder.Property(li => li.DeliveredQuantity).HasColumnType("decimal(18,2)");

            builder.Ignore(li => li.SubtotalAmount);
            builder.Ignore(li => li.TaxAmount);
            builder.Ignore(li => li.LineTotal);

            builder.HasIndex(li => li.SalesOrderId);
        }
    }
}
