using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class LeadScoringSettingsConfiguration : IEntityTypeConfiguration<LeadScoringSettings>
    {
        public void Configure(EntityTypeBuilder<LeadScoringSettings> builder)
        {
            builder.ToTable("LeadScoringSettings");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.ExpectedValueHighThreshold).HasColumnType("decimal(18,2)");
            builder.Property(s => s.ExpectedValueMediumThreshold).HasColumnType("decimal(18,2)");
        }
    }
}
