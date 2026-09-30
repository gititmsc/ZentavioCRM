using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities.Platform;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="TenantNote"/> -&gt; dbo.TenantNotes, in the Platform database
    /// only. Applied explicitly by <see cref="PlatformDbContext"/>, same as <see cref="TenantConfiguration"/>.
    /// </summary>
    public class TenantNoteConfiguration : IEntityTypeConfiguration<TenantNote>
    {
        public void Configure(EntityTypeBuilder<TenantNote> builder)
        {
            builder.ToTable("TenantNotes");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id).HasDefaultValueSql("NEWID()");

            builder.Property(n => n.Note).IsRequired().HasMaxLength(2000);
            builder.Property(n => n.CreatedAtUtc).IsRequired();

            builder.HasOne(n => n.Tenant)
                .WithMany()
                .HasForeignKey(n => n.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(n => n.CreatedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(n => n.TenantId);
            builder.HasIndex(n => n.CreatedAtUtc);
        }
    }
}
