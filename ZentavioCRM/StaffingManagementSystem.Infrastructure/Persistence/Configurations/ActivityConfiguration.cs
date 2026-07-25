using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
    {
        public void Configure(EntityTypeBuilder<Activity> builder)
        {
            builder.ToTable("Activities");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");

            builder.Property(a => a.Type).IsRequired().HasConversion<string>().HasMaxLength(30);
            builder.Property(a => a.RelatedToType).IsRequired().HasConversion<string>().HasMaxLength(30);

            builder.Property(a => a.Subject).IsRequired().HasMaxLength(200);
            builder.Property(a => a.Description).HasMaxLength(2000);

            builder.Property(a => a.CreatedAtUtc).IsRequired();

            builder.HasOne(a => a.AssignedToUser)
                .WithMany()
                .HasForeignKey(a => a.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(a => new { a.RelatedToType, a.RelatedToId });
        }
    }
}
