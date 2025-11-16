using Business.Interfaces.Implements.Business;
using Business.Services.Business;
using Data.Interfaz.IDataImplement.Business;
using Data.Services.Business;

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
