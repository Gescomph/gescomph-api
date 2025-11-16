using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Business
{
    public class PlazaConfiguration : BaseModelGenericConfiguration<Plaza>
    {
        public override void Configure(EntityTypeBuilder<Plaza> builder)
        {
            base.Configure(builder);

            builder.ToTable("Plazas", DatabaseSchemas.Business);

            builder.Property(p => p.Location)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasMany(p => p.Establishments)
                .WithOne(e => e.Plaza)
                .HasForeignKey(e => e.PlazaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.Location)
                .IsUnique();
        }
    }
}
