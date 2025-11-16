using Entity.Domain.Models.Implements.AdministrationSystem;

using Entity.DTOs.Implements.AdministrationSystem.Form;
using Entity.DTOs.Implements.AdministrationSystem.FormModule;
using Entity.DTOs.Implements.AdministrationSystem.Module;
using Entity.DTOs.Implements.AdministrationSystem.SystemParameter;
using Entity.DTOs.Implements.Utilities;
using Mapster;

namespace Business.Mapping.Registers
{
    public class AdministrationSystemMapping : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Form, FormSelectDto>();
            config.NewConfig<FormModule, FormModuleSelectDto>();
            config.NewConfig<Module, ModuleSelectDto>();
            config.NewConfig<SystemParameter, SystemParameterDto>();
            config.NewConfig<SystemParameter, SystemParameterUpdateDto>();



            config.NewConfig<Notification, NotificationDto>();
            config.NewConfig<NotificationCreateDto, Notification>()
                  .Ignore(dest => dest.Id)
                  .Ignore(dest => dest.Active)
                  .Ignore(dest => dest.IsDeleted)
                  .Ignore(dest => dest.CreatedAt)
                  .Ignore(dest => dest.ReadAt)
                  .Map(dest => dest.Title, src => src.Title.Trim())
                  .Map(dest => dest.Message, src => src.Message.Trim())
                  .Map(dest => dest.ActionRoute, src => string.IsNullOrWhiteSpace(src.ActionRoute) ? null : src.ActionRoute.Trim());

            config.NewConfig<NotificationUpdateDto, Notification>()
                  .Ignore(dest => dest.Active)
                  .Ignore(dest => dest.IsDeleted)
                  .Ignore(dest => dest.CreatedAt)
                  .Map(dest => dest.Title, src => src.Title.Trim())
                  .Map(dest => dest.Message, src => src.Message.Trim())
                  .Map(dest => dest.ActionRoute, src => string.IsNullOrWhiteSpace(src.ActionRoute) ? null : src.ActionRoute.Trim());
        }
    }
}

