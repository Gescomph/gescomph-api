using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Business
{
    public class EstablishmentConfiguration : IEntityTypeConfiguration<Establishment>
    {
        public void Configure(EntityTypeBuilder<Establishment> builder)
        {
            builder.ToTable("Establishments", DatabaseSchemas.Business);

            builder.Property(x => x.AreaM2)
                .HasPrecision(18, 2);

            builder.Property(x => x.RentValueBase)
                .HasPrecision(18, 2);

            builder.Property(x => x.UvtQty)
                .HasPrecision(18, 2);
        }
    }
}
