using Business.Interfaces.Implements.Persons;
using Business.Services.Persons;
using Data.Interfaz.IDataImplement.Persons;
using Data.Services.Persons;

namespace WebGESCOMPH.Extensions.Modules.Persons
{
    /// <summary>
    /// Registro DI del módulo de Personas.
    /// </summary>
    /// <remarks>
    /// Registra manualmente los servicios y repositorios del módulo Persons.
    /// </remarks>
    public static class PersonsModuleExtensions
    {
        public static IServiceCollection AddPersonsModule(this IServiceCollection services)
        {
            services.AddScoped<IPersonService, PersonService>();

            services.AddScoped<IPersonRepository, PersonRepository>();

            return services;
        }
    }
}
