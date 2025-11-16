using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Business
{
    public class ContractClauseConfiguration : BaseModelConfiguration<ContractClause>
    {
        public override void Configure(EntityTypeBuilder<ContractClause> builder)
        {
            base.Configure(builder);

            builder.ToTable("ContractClauses", DatabaseSchemas.Business);

            builder.HasOne(cc => cc.Contract)
                .WithMany(c => c.ContractClauses)
                .HasForeignKey(cc => cc.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cc => cc.Clause)
                .WithMany(c => c.ContractUsages)
                .HasForeignKey(cc => cc.ClauseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(cc => new { cc.ContractId, cc.ClauseId })
                .IsUnique();
        }
    }
}
