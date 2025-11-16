using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Business
{
    public class ClauseConfiguration : BaseModelGenericConfiguration<Clause>
    {
        public override void Configure(EntityTypeBuilder<Clause> builder)
        {
            base.Configure(builder);

            builder.ToTable("Clauses", DatabaseSchemas.Business);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(c => c.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.HasMany(c => c.ContractUsages)
                .WithOne(cc => cc.Clause)
                .HasForeignKey(cc => cc.ClauseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
