using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
    {
        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            builder.ToTable("CustomerAddresses");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");

            builder.Property(a => a.Type).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(a => a.Line1).IsRequired().HasMaxLength(200);
            builder.Property(a => a.Line2).HasMaxLength(200);
            builder.Property(a => a.City).HasMaxLength(100);
            builder.Property(a => a.State).HasMaxLength(100);
            builder.Property(a => a.Country).HasMaxLength(100);
            builder.Property(a => a.PostalCode).HasMaxLength(20);
        }
    }
}
