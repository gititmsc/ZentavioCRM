using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            builder.ToTable("Quotations");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Id).HasDefaultValueSql("NEWID()");

            builder.Property(q => q.QuotationNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(q => new { q.QuotationNumber, q.Version }).IsUnique();

            builder.Property(q => q.Status).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(q => q.TermsAndConditions).HasMaxLength(4000);

            builder.Property(q => q.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(q => q.TaxTotal).HasColumnType("decimal(18,2)");
            builder.Property(q => q.GrandTotal).HasColumnType("decimal(18,2)");

            builder.Property(q => q.CreatedAtUtc).IsRequired();

            builder.HasOne(q => q.Opportunity)
                .WithMany()
                .HasForeignKey(q => q.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(q => q.Customer)
                .WithMany()
                .HasForeignKey(q => q.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(q => q.AssignedToUser)
                .WithMany()
                .HasForeignKey(q => q.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(q => q.LineItems)
                .WithOne()
                .HasForeignKey(li => li.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(q => q.OpportunityId);
            builder.HasIndex(q => q.CustomerId);
            builder.HasIndex(q => q.Status);
            builder.HasIndex(q => q.AssignedToUserId);
        }
    }
}
