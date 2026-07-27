using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
    {
        public void Configure(EntityTypeBuilder<Opportunity> builder)
        {
            builder.ToTable("Opportunities");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id).HasDefaultValueSql("NEWID()");

            builder.Property(o => o.OpportunityNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(o => o.OpportunityNumber).IsUnique();

            builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
            builder.Property(o => o.Products).HasMaxLength(1000);
            builder.Property(o => o.Competitors).HasMaxLength(500);
            builder.Property(o => o.NextStep).HasMaxLength(300);
            builder.Property(o => o.LostReason).HasMaxLength(300);

            builder.Property(o => o.Stage).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(o => o.Value).HasColumnType("decimal(18,2)");

            builder.Property(o => o.CreatedAtUtc).IsRequired();

            builder.HasOne(o => o.Customer)
                .WithMany(c => c.Opportunities)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.AssignedToUser)
                .WithMany()
                .HasForeignKey(o => o.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(o => o.SourceLead)
                .WithMany()
                .HasForeignKey(o => o.SourceLeadId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(o => o.LineItems)
                .WithOne()
                .HasForeignKey(li => li.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(o => o.CustomerId);
            builder.HasIndex(o => o.Stage);
            builder.HasIndex(o => o.AssignedToUserId);
        }
    }
}
