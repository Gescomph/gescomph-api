using Business.Interfaces.Implements.Utilities;
using Business.Services.Utilities;
using Data.Interfaz.IDataImplement.Utilities;
using Data.Services.Utilities;

namespace WebGESCOMPH.Extensions.Modules.Utilities
{
    /// <summary>
    /// Registro DI del módulo de Utilidades (imágenes, etc.).
    /// </summary>
    /// <remarks>
    /// Registra manualmente los servicios y repositorios del módulo Utilities.
    /// </remarks>
    public static class UtilitiesModuleExtensions
    {
        public static IServiceCollection AddUtilitiesModule(this IServiceCollection services)
        {
            services.AddScoped<IImagesService, ImageService>();

            services.AddScoped<IImagesRepository, ImagesRepository>();

            return services;
        }
    }
}
