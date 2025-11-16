using Business.Interfaces.Implements.Location;
using Business.Services.Location;
using Data.Interfaz.IDataImplement.Location;
using Data.Services.Location;

namespace WebGESCOMPH.Extensions.Modules.Location
{
    /// <summary>
    /// Registro DI del módulo de Localización (Ciudades, Departamentos).
    /// </summary>
    /// <remarks>
    /// Registra manualmente servicios y repositorios del feature Location.
    /// </remarks>
    public static class LocationModuleExtensions
    {
        public static IServiceCollection AddLocationModule(this IServiceCollection services)
        {
            services.AddScoped<ICityService, CityService>();
            services.AddScoped<IDepartmentService, DepartmentService>();

            services.AddScoped<ICityRepository, CityRepository>();

            return services;
        }
    }
}
