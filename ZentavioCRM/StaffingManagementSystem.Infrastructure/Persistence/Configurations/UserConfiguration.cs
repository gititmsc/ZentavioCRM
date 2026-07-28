using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="User"/> -&gt; dbo.Users.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasDefaultValueSql("NEWID()");

            builder.Property(u => u.EmployeeCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(u => u.EmployeeCode)
                .IsUnique();

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.Mobile)
                .HasMaxLength(30);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.ReportingManager)
                .WithMany()
                .HasForeignKey(u => u.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Territory)
                .WithMany()
                .HasForeignKey(u => u.TerritoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(u => u.ProfilePhotoContent).HasColumnType("varbinary(max)");
            builder.Property(u => u.ProfilePhotoContentType).HasMaxLength(100);

            builder.Ignore(u => u.FullName);
        }
    }
}
