using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class LeadAssignmentRuleConfiguration : IEntityTypeConfiguration<LeadAssignmentRule>
    {
        public void Configure(EntityTypeBuilder<LeadAssignmentRule> builder)
        {
            builder.ToTable("LeadAssignmentRules");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id).HasDefaultValueSql("NEWID()");

            builder.Property(r => r.CreatedAtUtc).IsRequired();

            builder.HasOne(r => r.Territory)
                .WithMany()
                .HasForeignKey(r => r.TerritoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(r => r.LastAssignedUser)
                .WithMany()
                .HasForeignKey(r => r.LastAssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // At most one active rule should exist per territory (including one null-TerritoryId
            // fallback rule) — enforced in the service layer rather than a filtered unique index,
            // since SQL Server's filtered-unique-index support for "WHERE TerritoryId IS NULL" plus
            // IsActive together isn't worth the added migration complexity for a single-tenant-scale
            // settings table.
            builder.HasIndex(r => r.TerritoryId);
        }
    }
}
