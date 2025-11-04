using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.Implements.Utilities;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.DTOs.Implements.Business.Plaza;
using Entity.DTOs.Implements.Utilities.Images;
using Mapster;
using System.Collections.Generic;

namespace Business.Mapping.Registers
{
    public class BusinessEstablishmentMapping : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Establishment -> Select
            config.NewConfig<Establishment, EstablishmentSelectDto>()
                  .Map(dest => dest.Images, src => src.Images.Adapt<List<ImageSelectDto>>());

            // Establishment Create -> Entity
            config.NewConfig<EstablishmentCreateDto, Establishment>()
                  .Ignore(dest => dest.Id)
                  .Ignore(dest => dest.Images)
                  .Ignore(dest => dest.Active)
                  .Ignore(dest => dest.IsDeleted)
                  .Ignore(dest => dest.CreatedAt)
                  .IgnoreNullValues(true)
                  .Map(dest => dest.Name, src => src.Name.Trim())
                  .Map(dest => dest.Description, src => src.Description.Trim())
                  .Map(dest => dest.Address, src => string.IsNullOrWhiteSpace(src.Address) ? null : src.Address.Trim())
                  .Map(dest => dest.AreaM2, src => src.AreaM2)
                  .Map(dest => dest.UvtQty, src => src.UvtQty)
                  .Map(dest => dest.PlazaId, src => src.PlazaId);

            // Establishment Update -> Entity
            config.NewConfig<EstablishmentUpdateDto, Establishment>()
                  .Ignore(dest => dest.Images)
                  .Ignore(dest => dest.Active)
                  .Ignore(dest => dest.IsDeleted)
                  .Ignore(dest => dest.CreatedAt)
                  .IgnoreNullValues(true)
                  .Map(dest => dest.Name, src => src.Name == null ? null : src.Name.Trim())
                  .Map(dest => dest.Description, src => src.Description == null ? null : src.Description.Trim())
                  .Map(dest => dest.Address, src => string.IsNullOrWhiteSpace(src.Address) ? null : src.Address.Trim())
                  .Map(dest => dest.AreaM2, src => src.AreaM2)
                  .Map(dest => dest.RentValueBase, src => src.RentValueBase)
                  .Map(dest => dest.UvtQty, src => src.UvtQty)
                  .Map(dest => dest.PlazaId, src => src.PlazaId);

            // Plaza
            config.NewConfig<Plaza, PlazaSelectDto>();

            TypeAdapterConfig<(Plaza plaza, List<Images> images), PlazaSelectDto>
                .NewConfig()
                .Map(dst => dst.Id, src => src.plaza.Id)
                .Map(dst => dst.Name, src => src.plaza.Name)
                .Map(dst => dst.Description, src => src.plaza.Description)
                .Map(dst => dst.Location, src => src.plaza.Location)
                .Map(dst => dst.Active, src => src.plaza.Active)
                .Map(dst => dst.ImagePaths, src => src.images.Select(i => i.FilePath).ToList());

        }
    }
}

