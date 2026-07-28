using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class ContactPersonConfiguration : IEntityTypeConfiguration<ContactPerson>
    {
        public void Configure(EntityTypeBuilder<ContactPerson> builder)
        {
            builder.ToTable("ContactPersons");

            builder.HasKey(cp => cp.Id);

            builder.Property(cp => cp.Id).HasDefaultValueSql("NEWID()");

            builder.Property(cp => cp.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(cp => cp.LastName).HasMaxLength(100);
            builder.Property(cp => cp.Designation).HasMaxLength(100);
            builder.Property(cp => cp.Department).HasMaxLength(100);
            builder.Property(cp => cp.Email).HasMaxLength(256);
            builder.Property(cp => cp.Mobile).HasMaxLength(30);
            builder.Property(cp => cp.WhatsApp).HasMaxLength(30);
            builder.Property(cp => cp.LinkedIn).HasMaxLength(300);
            builder.Property(cp => cp.PreferredContactMethod).HasConversion<string>().HasMaxLength(20);

            builder.Property(cp => cp.CreatedAtUtc).IsRequired();

            builder.Ignore(cp => cp.FullName);
        }
    }
}
