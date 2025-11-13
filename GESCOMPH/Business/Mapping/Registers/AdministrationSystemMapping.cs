using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.CollectionSetting;
using Entity.DTOs.Implements.AdministrationSystem.Form;
using Entity.DTOs.Implements.AdministrationSystem.FormModule;
using Entity.DTOs.Implements.AdministrationSystem.Module;
using Entity.DTOs.Implements.AdministrationSystem.SystemParameter;

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

            config.NewConfig<CollectionSetting, CollectionSettingSelectDto>()
                .Map(dest => dest.TimeUnitName, src => src.TimeUnit.ToString());
            config.NewConfig<CollectionSetting, CollectionSettingUpdateDto>();

        }
    }
}

