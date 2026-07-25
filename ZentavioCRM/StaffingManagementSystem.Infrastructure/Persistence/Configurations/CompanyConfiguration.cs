using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id).HasDefaultValueSql("NEWID()");

            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
            builder.Property(c => c.LegalName).HasMaxLength(200);
            builder.Property(c => c.Industry).HasMaxLength(100);
            builder.Property(c => c.Website).HasMaxLength(300);
            builder.Property(c => c.Email).HasMaxLength(256);
            builder.Property(c => c.Phone).HasMaxLength(30);
            builder.Property(c => c.TaxNumber).HasMaxLength(50);
            builder.Property(c => c.AddressLine1).HasMaxLength(200);
            builder.Property(c => c.AddressLine2).HasMaxLength(200);
            builder.Property(c => c.City).HasMaxLength(100);
            builder.Property(c => c.State).HasMaxLength(100);
            builder.Property(c => c.Country).HasMaxLength(100);
            builder.Property(c => c.PostalCode).HasMaxLength(20);
            builder.Property(c => c.DefaultCurrency).IsRequired().HasMaxLength(10);
            builder.Property(c => c.TimeZone).IsRequired().HasMaxLength(100);
            builder.Property(c => c.LogoUrl).HasMaxLength(500);

            builder.Property(c => c.CreatedAtUtc).IsRequired();
        }
    }
}
