using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.DataInit.AdministrationSystem
{
    public class CollectionSettingSeeder : IEntityTypeConfiguration<CollectionSetting>
    {
        public void Configure(EntityTypeBuilder<CollectionSetting> builder)
        {
            var seedDate = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new CollectionSetting
                {
                    // Primera fase: AvisoPrejuridico - 3 minutos
                    Id = 1,
                    Name = "AvisoPrejuridico",
                    Value = 3,
                    TimeUnit = TimeUnit.Minutes,
                    Description = "Tiempo antes de iniciar cobro coactivo",
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new CollectionSetting
                {
                    // Segunda fase: CobroCoactivo - 5 minutos
                    Id = 2,
                    Name = "CobroCoactivo",
                    Value = 5,
                    TimeUnit = TimeUnit.Minutes,
                    Description = "Paso a coactivo",
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new CollectionSetting
                {
                    // Tercera fase: CobroJuridico - 8 minutos
                    Id = 3,
                    Name = "CobroJuridico",
                    Value = 8,
                    TimeUnit = TimeUnit.Minutes,
                    Description = "Paso a jurídico",
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                }
            );
        }
    }
}
