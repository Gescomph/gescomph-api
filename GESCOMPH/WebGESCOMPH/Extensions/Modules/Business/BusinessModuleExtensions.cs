using Business.Interfaces.Implements.Business;
using Business.Services.Business;
using Business.Services.AdministrationSystem;
using Data.Interfaz.IDataImplement.Business;
using Data.Interfaz.IDataImplement.AdministrationSystem;
using Data.Services.Business;
using Data.Interfaz.DataBasic;
using Business.Interfaces.Implements.AdministrationSystem;

namespace WebGESCOMPH.Extensions.Modules.Business
{
    /// <summary>
    /// Registro DI del módulo de negocio (contratos, citas, plazas, etc.).
    /// </summary>
    /// <remarks>
    /// Qué hace: registra explícitamente servicios y repositorios del feature Business.
    /// Por qué: mantener una convención DRY y robusta en el armado de módulos.
    /// Para qué: habilitar todas las capacidades de dominio de negocio.
    /// </remarks>
    public static class BusinessModuleExtensions
    {
        public static IServiceCollection AddBusinessModule(this IServiceCollection services)
        {
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IClauseService, ClauseService>();
            services.AddScoped<IContractClauseService, ContractClauseService>();
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IEstablishmentService, EstablishmentService>();
            services.AddScoped<IObligationMonthService, ObligationMonthService>();
            services.AddScoped<IPlazaService, PlazaService>();
            services.AddScoped<ICollectionService, CollectionService>();
            services.AddScoped<ICollectionSettingServices, CollectionSettingServices>();

            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IEstablishmentsRepository, EstablishmentsRepository>();
            services.AddScoped<IObligationMonthRepository, ObligationMonthRepository>();
            services.AddScoped<IPlazaRepository, PlazaRepository>();
            services.AddScoped<IPremisesLeasedRepository, PremisesLeasedRepository>();

            return services;
        }
    }
}
