using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities.Platform;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="TenantPayment"/> -&gt; dbo.TenantPayments, in the Platform
    /// database only. Applied explicitly by <see cref="PlatformDbContext"/>, same as
    /// <see cref="TenantConfiguration"/>.
    /// </summary>
    public class TenantPaymentConfiguration : IEntityTypeConfiguration<TenantPayment>
    {
        public void Configure(EntityTypeBuilder<TenantPayment> builder)
        {
            builder.ToTable("TenantPayments");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id).HasDefaultValueSql("NEWID()");

            builder.Property(p => p.Amount).IsRequired().HasColumnType("decimal(12,2)");
            builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
            builder.Property(p => p.PaidAtUtc).IsRequired();
            builder.Property(p => p.Note).HasMaxLength(1000);
            builder.Property(p => p.CreatedAtUtc).IsRequired();

            builder.HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.RecordedByAdmin)
                .WithMany()
                .HasForeignKey(p => p.RecordedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(p => p.TenantId);
            builder.HasIndex(p => p.PaidAtUtc);
        }
    }
}
