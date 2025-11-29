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
                    Id = 1,
                    Name = "PreDueDays",
                    Value = 3,
                    TimeUnit = TimeUnit.Minutes,
                    Description = "Tiempo antes del vencimiento para enviar aviso previo.",
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new CollectionSetting
                {
                    Id = 2,
                    Name = "OverdueDays",
                    Value = 0,
                    TimeUnit = TimeUnit.Minutes,
                    Description = "Tiempo para considerar una obligación en mora después del vencimiento.",
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new CollectionSetting
                {
                    Id = 3,
                    Name = "DailyLateFeeInterval",
                    Value = 1,
                    TimeUnit = TimeUnit.Minutes,
                    Description = "Intervalo para recalcular la mora acumulada.",
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new CollectionSetting
                {
                    Id = 4,
                    Name = "LateFeeDayUnit",
                    Value = 1,
                    TimeUnit = TimeUnit.Minutes,
                    Description = "Unidad base para cálculo de mora.",
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                }
            );
        }
    }
}
