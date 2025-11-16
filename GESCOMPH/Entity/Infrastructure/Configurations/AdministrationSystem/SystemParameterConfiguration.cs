using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.AdministrationSystem
{
    public class SystemParameterConfiguration : BaseModelConfiguration<SystemParameter>
    {
        public override void Configure(EntityTypeBuilder<SystemParameter> builder)
        {
            base.Configure(builder);

            builder.ToTable("SystemParameters", DatabaseSchemas.AdministrationSystem);

            builder.Property(sp => sp.Key)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sp => sp.Value)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(sp => sp.EffectiveFrom)
                .IsRequired();

            builder.Property(sp => sp.EffectiveTo);

            builder.HasIndex(sp => sp.Key)
                .IsUnique();
        }
    }
}
