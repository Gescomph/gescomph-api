using WebGESCOMPH.Middleware;
using WebGESCOMPH.Middleware.Handlers;

namespace WebGESCOMPH.Extensions.Modules.Exceptions
{
    /// <summary>
    /// Registro DI de handlers de excepciones personalizados del middleware.
    /// </summary>
    /// <remarks>
    /// Registra manualmente cada handler concreto de excepción como singleton para que el middleware pueda resolverlos.
    /// </remarks>
    public static class ExceptionHandlersModuleExtensions
    {
        public static IServiceCollection AddExceptionHandlersModule(this IServiceCollection services)
        {
            services.AddSingleton<IExceptionHandler, BusinessExceptionHandler>();
            services.AddSingleton<IExceptionHandler, DbConcurrencyExceptionHandler>();
            services.AddSingleton<IExceptionHandler, DbUpdateExceptionHandler>();
            services.AddSingleton<IExceptionHandler, DefaultExceptionHandler>();
            services.AddSingleton<IExceptionHandler, EntityNotFoundExceptionHandler>();
            services.AddSingleton<IExceptionHandler, ExternalServiceExceptionHandler>();
            services.AddSingleton<IExceptionHandler, FileAccessExceptionHandler>();
            services.AddSingleton<IExceptionHandler, ForbiddenExceptionHandler>();
            services.AddSingleton<IExceptionHandler, HttpRequestExceptionHandler>();
            services.AddSingleton<IExceptionHandler, InfrastructureExceptionHandler>();
            services.AddSingleton<IExceptionHandler, JsonParsingExceptionHandler>();
            services.AddSingleton<IExceptionHandler, NullReferenceExceptionHandler>();
            services.AddSingleton<IExceptionHandler, RateLimitExceptionHandler>();
            services.AddSingleton<IExceptionHandler, SecurityTokenExceptionHandler>();
            services.AddSingleton<IExceptionHandler, TimeoutExceptionHandler>();
            services.AddSingleton<IExceptionHandler, UnauthorizedAccessHandler>();
            services.AddSingleton<IExceptionHandler, ValidationExceptionHandler>();

            return services;
        }
    }
}
