using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class LeadAssignmentRuleUserConfiguration : IEntityTypeConfiguration<LeadAssignmentRuleUser>
    {
        public void Configure(EntityTypeBuilder<LeadAssignmentRuleUser> builder)
        {
            builder.ToTable("LeadAssignmentRuleUsers");

            builder.HasKey(ru => new { ru.RuleId, ru.UserId });

            builder.HasOne(ru => ru.Rule)
                .WithMany(r => r.EligibleUsers)
                .HasForeignKey(ru => ru.RuleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ru => ru.User)
                .WithMany()
                .HasForeignKey(ru => ru.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
