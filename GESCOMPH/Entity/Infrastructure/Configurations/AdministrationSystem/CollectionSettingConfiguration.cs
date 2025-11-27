using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.AdministrationSystem
{
    public class CollectionSettingConfiguration : BaseModelGenericConfiguration<CollectionSetting>
    {
        public override void Configure(EntityTypeBuilder<CollectionSetting> builder)
        {
            base.Configure(builder);
            builder.ToTable("CollectionSettings", DatabaseSchemas.AdministrationSystem);
            // Map enum as integer
            builder.Property(cs => cs.TimeUnit)
                .HasConversion<int>()
                .IsRequired();
            builder.Property(cs => cs.Value)
                .IsRequired();
        }
    }
}
