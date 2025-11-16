using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Business
{
    public class PremisesLeasedConfiguration : BaseModelConfiguration<PremisesLeased>
    {
        public override void Configure(EntityTypeBuilder<PremisesLeased> builder)
        {
            base.Configure(builder);

            builder.ToTable("PremisesLeaseds", DatabaseSchemas.Business);

            builder.HasOne(pl => pl.Contract)
                .WithMany(c => c.PremisesLeased)
                .HasForeignKey(pl => pl.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pl => pl.Establishment)
                .WithMany(e => e.PremisesLeased)
                .HasForeignKey(pl => pl.EstablishmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pl => new { pl.ContractId, pl.EstablishmentId })
                .IsUnique();

            builder.HasIndex(pl => pl.EstablishmentId);
        }
    }
}
