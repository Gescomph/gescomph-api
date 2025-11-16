using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Services.SecurityAuthentication;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Data.Interfaz.Security;
using Data.Services.AdministrationSystem;
using Data.Services.SecurityAuthentication;

namespace WebGESCOMPH.Extensions.Modules.Security
{
    /// <summary>
    /// Registro DI del módulo de Seguridad/Autenticación.
    /// </summary>
    /// <remarks>
    /// Incluye servicios de autenticación y repositorios tanto en Services.SecurityAuthentication como
    /// en Repositories.Implementations.SecurityAuthentication.
    /// </remarks>
    public static class SecurityAuthenticationModuleExtensions
    {
        public static IServiceCollection AddSecurityAuthenticationModule(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRolFormPermissionService, RolFormPermissionService>();
            services.AddScoped<IRolService, RolService>();
            services.AddScoped<IRolUserService, RolUserService>();
            services.AddScoped<IUserContextService, UserContextService>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IPasswordResetCodeRepository, PasswordResetCodeRepository>();
            services.AddScoped<ITwoFactorCodeRepository, TwoFactorCodeRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IRolFormPermissionRepository, RolFormPermissionRepository>();
            services.AddScoped<IRolUserRepository, RolUserRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserMeRepository, MeRepository>();

            return services;
        }
    }
}
