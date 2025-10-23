using System.Linq;

using Entity.Domain.Models.Implements.SecurityAuthentication;

using Entity.DTOs.Implements.SecurityAuthentication.Permission;
using Entity.DTOs.Implements.SecurityAuthentication.Rol;
using Entity.DTOs.Implements.SecurityAuthentication.RolFormPemission;
using Entity.DTOs.Implements.SecurityAuthentication.RolUser;
using Entity.DTOs.Implements.SecurityAuthentication.User;

using Mapster;

namespace Business.Mapping.Registers
{
    public class SecurityAuthenticationMapping : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Permission, PermissionSelectDto>();
            config.NewConfig<Rol, RolSelectDto>();
            config.NewConfig<RolFormPermission, RolFormPermissionSelectDto>();
            config.NewConfig<RolUser, RolUserSelectDto>();

            config.NewConfig<User, UserSelectDto>()
               .Map(dest => dest.PersonId, src => src.PersonId)
               .Map(dest => dest.PersonFirstName, src => src.Person != null ? src.Person.FirstName ?? string.Empty : string.Empty)
               .Map(dest => dest.PersonLastName, src => src.Person != null ? src.Person.LastName ?? string.Empty : string.Empty)
               .Map(dest => dest.PersonName, src => src.Person != null
                    ? $"{(src.Person.FirstName ?? string.Empty).Trim()} {(src.Person.LastName ?? string.Empty).Trim()}".Trim()
                    : string.Empty)
               .Map(dest => dest.PersonDocument, src => src.Person != null ? src.Person.Document ?? string.Empty : string.Empty)
               .Map(dest => dest.PersonAddress, src => src.Person != null ? src.Person.Address ?? string.Empty : string.Empty)
               .Map(dest => dest.PersonPhone, src => src.Person != null ? src.Person.Phone ?? string.Empty : string.Empty)
               .Map(dest => dest.CityId, src => src.Person != null ? src.Person.CityId : 0)
               .Map(dest => dest.CityName, src => src.Person != null && src.Person.City != null
                    ? src.Person.City.Name ?? string.Empty
                    : string.Empty)
               .Map(dest => dest.Email, src => src.Email)
               .Map(dest => dest.Active, src => src.Active)
               .Map(dest => dest.CreatedAt, src => src.CreatedAt)
               .Map(dest => dest.Roles, src => src.RolUsers
                    .Where(ru => ru.Rol != null && !string.IsNullOrWhiteSpace(ru.Rol.Name))
                    .Select(ru => ru.Rol!.Name));

            config.NewConfig<UserCreateDto, User>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Password);

            config.NewConfig<UserUpdateDto, User>()
                .Ignore(dest => dest.Password);
        }
    }
}

