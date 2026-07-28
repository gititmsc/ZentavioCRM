using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
    {
        public void Configure(EntityTypeBuilder<SalesOrder> builder)
        {
            builder.ToTable("SalesOrders");

            builder.HasKey(so => so.Id);

            builder.Property(so => so.Id).HasDefaultValueSql("NEWID()");

            builder.Property(so => so.SalesOrderNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(so => so.SalesOrderNumber).IsUnique();

            builder.Property(so => so.Status).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(so => so.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(so => so.TaxTotal).HasColumnType("decimal(18,2)");
            builder.Property(so => so.GrandTotal).HasColumnType("decimal(18,2)");

            builder.Property(so => so.CreatedAtUtc).IsRequired();

            // One quotation converts to at most one sales order — enforced via unique FK index.
            builder.HasOne(so => so.Quotation)
                .WithMany()
                .HasForeignKey(so => so.QuotationId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(so => so.QuotationId).IsUnique();

            builder.HasOne(so => so.Customer)
                .WithMany()
                .HasForeignKey(so => so.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(so => so.AssignedToUser)
                .WithMany()
                .HasForeignKey(so => so.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(so => so.LineItems)
                .WithOne()
                .HasForeignKey(li => li.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(so => so.CustomerId);
            builder.HasIndex(so => so.Status);
            builder.HasIndex(so => so.AssignedToUserId);
        }
    }
}
