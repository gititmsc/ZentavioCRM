using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class SavedAnalyticsItemConfiguration : IEntityTypeConfiguration<SavedAnalyticsItem>
    {
        public void Configure(EntityTypeBuilder<SavedAnalyticsItem> builder)
        {
            builder.ToTable("SavedAnalyticsItems");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id).HasDefaultValueSql("NEWID()");

            builder.Property(i => i.Name).IsRequired().HasMaxLength(150);

            builder.Property(i => i.Description).HasMaxLength(500);

            builder.Property(i => i.ConfigJson).IsRequired();

            builder.Property(i => i.CreatedAtUtc).IsRequired();

            // Owner is a plain Restrict FK: deactivate users instead of deleting them (same convention as every
            // other *UserId FK in this schema), so a delete attempt surfaces rather than silently cascading away
            // someone's saved work.
            builder.HasOne(i => i.OwnerUser)
                .WithMany()
                .HasForeignKey(i => i.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(i => new { i.Kind, i.OwnerUserId });
            builder.HasIndex(i => new { i.Kind, i.IsShared });
        }
    }
}
