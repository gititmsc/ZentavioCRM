using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class OpportunityLineItemConfiguration : IEntityTypeConfiguration<OpportunityLineItem>
    {
        public void Configure(EntityTypeBuilder<OpportunityLineItem> builder)
        {
            builder.ToTable("OpportunityLineItems");

            builder.HasKey(li => li.Id);

            builder.Property(li => li.Id).HasDefaultValueSql("NEWID()");

            builder.Property(li => li.ProductName).IsRequired().HasMaxLength(200);
            builder.Property(li => li.Quantity).HasColumnType("decimal(18,2)");
            builder.Property(li => li.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(li => li.DiscountPercent).HasColumnType("decimal(5,2)");

            // Computed client-side only (Quantity * UnitPrice * (1 - Discount%)) — not persisted.
            builder.Ignore(li => li.LineTotal);

            builder.HasIndex(li => li.OpportunityId);
        }
    }
}
