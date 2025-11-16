using FluentValidation;
using System.Reflection;

namespace WebGESCOMPH.Extensions.Validation
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddValidations(this IServiceCollection services)
        {
            // Registra todos los IValidator<T> desde el ensamblado de Entity
            var entityAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Entity")
                ?? Assembly.Load("Entity");

            services.AddValidatorsFromAssembly(entityAssembly);

            return services;
        }
    }
}

