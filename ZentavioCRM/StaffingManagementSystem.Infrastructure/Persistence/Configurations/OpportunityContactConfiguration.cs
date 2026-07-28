using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class OpportunityContactConfiguration : IEntityTypeConfiguration<OpportunityContact>
    {
        public void Configure(EntityTypeBuilder<OpportunityContact> builder)
        {
            builder.ToTable("OpportunityContacts");

            builder.HasKey(oc => oc.Id);

            builder.Property(oc => oc.Id).HasDefaultValueSql("NEWID()");

            builder.Property(oc => oc.Role).IsRequired().HasConversion<string>().HasMaxLength(30);
            builder.Property(oc => oc.Notes).HasMaxLength(500);

            // OpportunityId FK/cascade is configured from the Opportunity side (HasMany(o => o.Contacts) in
            // OpportunityConfiguration) — only the ContactPerson side is configured here.
            builder.HasOne(oc => oc.ContactPerson)
                .WithMany()
                .HasForeignKey(oc => oc.ContactPersonId)
                .OnDelete(DeleteBehavior.Cascade);

            // One role-row per contact per opportunity — the same contact can't be added twice to the
            // same deal's buying committee (edit the existing row's Role instead of adding a duplicate).
            builder.HasIndex(oc => new { oc.OpportunityId, oc.ContactPersonId }).IsUnique();
            builder.HasIndex(oc => oc.ContactPersonId);
        }
    }
}
