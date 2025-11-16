using Business.Interfaces.Implements.AdministrationSystem;
using Business.Services.AdministrationSystem;
using Data.Interfaz.IDataImplement.AdministrationSystem;
using Data.Services.AdministrationSystem;

namespace WebGESCOMPH.Extensions.Modules.Administration
{
    /// <summary>
    /// Registro DI del módulo de Administración del Sistema.
    /// </summary>
    /// <remarks>
    /// Qué hace: registra servicios de Business.Services.AdministrationSystem y repos de
    /// Data.Services.AdministratiosSystem (se respeta la tipografía del namespace existente).
    /// Por qué: aislar el alta por feature y encapsular el typo histórico del namespace de Data.
    /// Para qué: activar funcionalidades de administración en AddApplicationServices.
    /// </remarks>
    public static class AdministrationSystemModuleExtensions
    {
        public static IServiceCollection AddAdministrationSystemModule(this IServiceCollection services)
        {
            services.AddScoped<IFormMouduleService, FormModuleService>();
            services.AddScoped<IFormService, FormService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISystemParameterService, SystemParameterService>();

            services.AddScoped<IFormModuleRepository, FormModuleRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();

            return services;
        }
    }
}
