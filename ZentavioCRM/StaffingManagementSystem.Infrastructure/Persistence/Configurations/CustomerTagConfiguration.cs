using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class CustomerTagConfiguration : IEntityTypeConfiguration<CustomerTag>
    {
        public void Configure(EntityTypeBuilder<CustomerTag> builder)
        {
            builder.ToTable("CustomerTags");

            builder.HasKey(ct => new { ct.CustomerId, ct.TagId });

            builder.HasOne(ct => ct.Customer)
                .WithMany()
                .HasForeignKey(ct => ct.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ct => ct.Tag)
                .WithMany()
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
