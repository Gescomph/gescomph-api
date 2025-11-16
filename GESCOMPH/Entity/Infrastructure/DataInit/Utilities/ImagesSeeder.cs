using Entity.Domain.Models.Implements.Utilities;
using Entity.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.DataInit.Utilities
{
    public class ImagesSeeder : IEntityTypeConfiguration<Images>
    {
        public void Configure(EntityTypeBuilder<Images> builder)
        {
            var seedDate = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new Images
                {
                    Id = 1,
                    FileName = "primavera_1.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "primavera_1",
                    EntityType = EntityType.Establishment,
                    EntityId = 1,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 2,
                    FileName = "primavera_2.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "primavera_2",
                    EntityType = EntityType.Establishment,
                    EntityId = 1,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 3,
                    FileName = "torre_1.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "torre_1",
                    EntityType = EntityType.Establishment,
                    EntityId = 2,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 4,
                    FileName = "torre_2.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "torre_2",
                    EntityType = EntityType.Establishment,
                    EntityId = 2,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 5,
                    FileName = "bodega_1.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "bodega_1",
                    EntityType = EntityType.Establishment,
                    EntityId = 3,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 6,
                    FileName = "bodega_2.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "bodega_2",
                    EntityType = EntityType.Establishment,
                    EntityId = 3,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 7,
                    FileName = "local_1.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "local_1",
                    EntityType = EntityType.Establishment,
                    EntityId = 4,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 8,
                    FileName = "local_2.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "local_2",
                    EntityType = EntityType.Establishment,
                    EntityId = 4,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 9,
                    FileName = "isla_1.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "isla_1",
                    EntityType = EntityType.Establishment,
                    EntityId = 5,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 10,
                    FileName = "isla_2.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "isla_2",
                    EntityType = EntityType.Establishment,
                    EntityId = 5,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 11,
                    FileName = "oficina12_1.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "oficina12_1",
                    EntityType = EntityType.Establishment,
                    EntityId = 6,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                },
                new Images
                {
                    Id = 12,
                    FileName = "oficina12_2.jpg",
                    FilePath = "https://res.cloudinary.com/dmbndpjlh/image/upload/v1755031443/defaul_cj5nqv.png",
                    PublicId = "oficina12_2",
                    EntityType = EntityType.Establishment,
                    EntityId = 6,
                    Active = true,
                    IsDeleted = false,
                    CreatedAt = seedDate
                }
            );
        }
    }
}
