using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class UserDelegationConfiguration : IEntityTypeConfiguration<UserDelegation>
    {
        public void Configure(EntityTypeBuilder<UserDelegation> builder)
        {
            builder.ToTable("UserDelegations");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id).HasDefaultValueSql("NEWID()");

            builder.Property(d => d.StartDateUtc).IsRequired();
            builder.Property(d => d.EndDateUtc).IsRequired();
            builder.Property(d => d.Notes).HasMaxLength(500);
            builder.Property(d => d.CreatedAtUtc).IsRequired();

            // Both FKs point to Users; Restrict on both (rather than Cascade) avoids SQL Server's
            // multiple-cascade-paths error and matches every other User self-reference in this app
            // (ReportingManager, etc.) — a delegation record simply blocks deleting either user
            // involved until the delegation itself is removed.
            builder.HasOne(d => d.DelegatorUser)
                .WithMany()
                .HasForeignKey(d => d.DelegatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.DelegateUser)
                .WithMany()
                .HasForeignKey(d => d.DelegateUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.DelegatorUserId);
            builder.HasIndex(d => d.DelegateUserId);
            builder.HasIndex(d => new { d.StartDateUtc, d.EndDateUtc });
        }
    }
}
