using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Business
{
    public class ContractConfiguration : IEntityTypeConfiguration<Contract>
    {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            builder.ToTable("Contracts", DatabaseSchemas.Business);

            builder.Property(x => x.TotalBaseRentAgreed)
                   .HasPrecision(18, 2);

            builder.Property(x => x.TotalUvtQtyAgreed)
                   .HasPrecision(18, 2);
        }
    }
}
