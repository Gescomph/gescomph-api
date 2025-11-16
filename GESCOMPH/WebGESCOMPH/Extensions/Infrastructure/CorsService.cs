namespace WebGESCOMPH.Extensions.Infrastructure
{
    public static class CorsService
    {
        public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
        {
            var fromConfig = (configuration.GetValue<string>("OrigenesPermitidos") ?? "")
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .SetIsOriginAllowed(origin =>
                        {
                            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                                uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }

                            return fromConfig.Contains(origin, StringComparer.OrdinalIgnoreCase);
                        })
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }
    }

}

