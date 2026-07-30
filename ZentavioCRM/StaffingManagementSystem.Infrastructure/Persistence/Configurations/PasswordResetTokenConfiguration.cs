using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("PasswordResetTokens");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasDefaultValueSql("NEWID()");

            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            builder.Property(t => t.ExpiresAtUtc).IsRequired();
            builder.Property(t => t.CreatedAtUtc).IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => t.TokenHash).IsUnique();
            builder.HasIndex(t => t.UserId);
        }
    }
}
