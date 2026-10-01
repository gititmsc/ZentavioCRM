using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class LeadAssignmentSettingsConfiguration : IEntityTypeConfiguration<LeadAssignmentSettings>
    {
        public void Configure(EntityTypeBuilder<LeadAssignmentSettings> builder)
        {
            builder.ToTable("LeadAssignmentSettings");

            builder.HasKey(s => s.Id);
        }
    }
}
