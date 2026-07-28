using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class TerritoryConfiguration : IEntityTypeConfiguration<Territory>
    {
        public void Configure(EntityTypeBuilder<Territory> builder)
        {
            builder.ToTable("Territories");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasDefaultValueSql("NEWID()");

            builder.Property(t => t.Name).IsRequired().HasMaxLength(150);

            builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(t => t.CreatedAtUtc).IsRequired();

            builder.HasOne(t => t.ParentTerritory)
                .WithMany(t => t.ChildTerritories)
                .HasForeignKey(t => t.ParentTerritoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => t.Name);
        }
    }
}
