using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class LeadTagConfiguration : IEntityTypeConfiguration<LeadTag>
    {
        public void Configure(EntityTypeBuilder<LeadTag> builder)
        {
            builder.ToTable("LeadTags");

            builder.HasKey(lt => new { lt.LeadId, lt.TagId });

            builder.HasOne(lt => lt.Lead)
                .WithMany()
                .HasForeignKey(lt => lt.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(lt => lt.Tag)
                .WithMany()
                .HasForeignKey(lt => lt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
