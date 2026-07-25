using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id).HasDefaultValueSql("NEWID()");

            builder.Property(d => d.Name).IsRequired().HasMaxLength(150);

            builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(d => d.CreatedAtUtc).IsRequired();

            builder.HasOne(d => d.Company)
                .WithMany(c => c.Departments)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.ParentDepartment)
                .WithMany(d => d.ChildDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => new { d.CompanyId, d.Name });
        }
    }
}
