using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class LeadConfiguration : IEntityTypeConfiguration<Lead>
    {
        public void Configure(EntityTypeBuilder<Lead> builder)
        {
            builder.ToTable("Leads");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id).HasDefaultValueSql("NEWID()");

            builder.Property(l => l.LeadNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(l => l.LeadNumber).IsUnique();

            builder.Property(l => l.CompanyName).IsRequired().HasMaxLength(200);
            builder.Property(l => l.ContactName).IsRequired().HasMaxLength(200);
            builder.Property(l => l.Email).HasMaxLength(256);
            builder.Property(l => l.Mobile).HasMaxLength(30);
            builder.Property(l => l.Industry).HasMaxLength(100);
            builder.Property(l => l.Campaign).HasMaxLength(150);
            builder.Property(l => l.UtmSource).HasMaxLength(150);
            builder.Property(l => l.UtmMedium).HasMaxLength(150);
            builder.Property(l => l.UtmCampaign).HasMaxLength(150);
            builder.Property(l => l.UtmTerm).HasMaxLength(150);
            builder.Property(l => l.UtmContent).HasMaxLength(150);
            builder.Property(l => l.Timeline).HasMaxLength(100);
            builder.Property(l => l.Territory).HasMaxLength(100);
            builder.Property(l => l.LostReason).HasMaxLength(300);

            builder.Property(l => l.Source).IsRequired().HasConversion<string>().HasMaxLength(30);
            builder.Property(l => l.Status).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(l => l.Budget).HasColumnType("decimal(18,2)");
            builder.Property(l => l.ExpectedValue).HasColumnType("decimal(18,2)");

            builder.Property(l => l.CreatedAtUtc).IsRequired();

            builder.HasOne(l => l.AssignedToUser)
                .WithMany()
                .HasForeignKey(l => l.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(l => l.ConvertedCustomer)
                .WithMany(c => c.ConvertedFromLeads)
                .HasForeignKey(l => l.ConvertedCustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(l => l.LinkedCustomer)
                .WithMany()
                .HasForeignKey(l => l.LinkedCustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            // NOTE: this configures EF's model/graph shape only — there is deliberately NO matching
            // database-level FOREIGN KEY constraint for LinkedContactId (see SQL Changes/TenantSchema.sql).
            // dbo.ContactPersons already cascade-deletes from dbo.Customers, and LinkedCustomerId above
            // already SET NULLs from dbo.Customers directly, so a real FK here would be a second,
            // longer SET NULL path into Leads that SQL Server refuses to create ("multiple cascade
            // paths", Msg 1785). LeadService.SyncLinkedContactAsync tolerates a stale/missing
            // LinkedContactId gracefully, so this stays an application-enforced reference only.
            builder.HasOne(l => l.LinkedContact)
                .WithMany()
                .HasForeignKey(l => l.LinkedContactId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(l => l.TerritoryRef)
                .WithMany()
                .HasForeignKey(l => l.TerritoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(l => l.Status);
            builder.HasIndex(l => l.AssignedToUserId);
            builder.HasIndex(l => l.TerritoryId);
            builder.HasIndex(l => l.LinkedCustomerId);
            builder.HasIndex(l => l.LinkedContactId);
        }
    }
}
