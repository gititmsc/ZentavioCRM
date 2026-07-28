using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id).HasDefaultValueSql("NEWID()");

            builder.Property(d => d.EntityType).IsRequired().HasMaxLength(50);
            builder.Property(d => d.FileName).IsRequired().HasMaxLength(260);
            builder.Property(d => d.ContentType).IsRequired().HasMaxLength(150);
            builder.Property(d => d.Content).IsRequired().HasColumnType("varbinary(max)");

            builder.Property(d => d.CreatedAtUtc).IsRequired();

            builder.HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(d => new { d.EntityType, d.EntityId });
        }
    }
}
