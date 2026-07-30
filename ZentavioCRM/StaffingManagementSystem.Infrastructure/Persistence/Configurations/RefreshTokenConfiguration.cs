using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasDefaultValueSql("NEWID()");

            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            builder.Property(t => t.ExpiresAtUtc).IsRequired();
            builder.Property(t => t.CreatedAtUtc).IsRequired();
            builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);

            // Restrict (not Cascade) matches every other User FK in this app (ReportingManager,
            // UserDelegation, etc.) — Users have no delete feature anyway, so this never actually
            // blocks anything in practice; it just keeps every User FK in the app consistent.
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => t.TokenHash).IsUnique();
            builder.HasIndex(t => t.UserId);
        }
    }
}
