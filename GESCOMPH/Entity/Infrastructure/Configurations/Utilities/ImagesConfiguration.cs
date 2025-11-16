using Entity.Domain.Models.Implements.Utilities;
using Entity.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Utilities
{
    public class ImagesConfiguration : IEntityTypeConfiguration<Images>
    {
        public void Configure(EntityTypeBuilder<Images> builder)
        {

            builder.HasIndex(x => new { x.EntityType, x.EntityId });

            builder.ToTable("Images", DatabaseSchemas.Utilities);

            builder.HasKey(i => i.Id);

            builder.Property(i => i.FileName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(i => i.FilePath)
                   .IsRequired()
                   .HasMaxLength(512);

            builder.Property(i => i.PublicId)
                   .IsRequired()
                   .HasMaxLength(128);


        }
    }
}
