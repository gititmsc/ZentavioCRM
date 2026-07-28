using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id).HasDefaultValueSql("NEWID()");

            builder.Property(c => c.CustomerNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(c => c.CustomerNumber).IsUnique();

            builder.Property(c => c.Type).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(c => c.LegalName).IsRequired().HasMaxLength(200);
            builder.Property(c => c.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Industry).HasMaxLength(100);
            builder.Property(c => c.Website).HasMaxLength(300);
            builder.Property(c => c.Email).HasMaxLength(256);
            builder.Property(c => c.Phone).HasMaxLength(30);
            builder.Property(c => c.TaxNumber).HasMaxLength(50);
            builder.Property(c => c.CurrencyCode).IsRequired().HasMaxLength(10);
            builder.Property(c => c.Rating).HasMaxLength(20);
            builder.Property(c => c.Tags).HasMaxLength(500);
            builder.Property(c => c.AcquisitionSource).HasConversion<string>().HasMaxLength(30);
            builder.Property(c => c.HealthStatus).HasConversion<string>().HasMaxLength(20);

            builder.Property(c => c.AnnualRevenue).HasColumnType("decimal(18,2)");
            builder.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");

            builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
            builder.Property(c => c.CreatedAtUtc).IsRequired();

            builder.HasOne(c => c.AssignedToUser)
                .WithMany()
                .HasForeignKey(c => c.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.Contacts)
                .WithOne(cp => cp.Customer)
                .HasForeignKey(cp => cp.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Addresses)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => c.DisplayName);
        }
    }
}
