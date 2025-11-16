using Hangfire;
using Hangfire.SqlServer;
using TimeZoneConverter;
using WebGESCOMPH.RealTime.Contract;
using WebGESCOMPH.RealTime.Obligations;
using WebGESCOMPH.Security;

namespace WebGESCOMPH.Extensions.Infrastructure
{
    /// <summary>
    /// Configura Hangfire (storage, servidor, dashboard y jobs recurrentes).
    /// </summary>
    public static class HangfireExtensions
    {
        /// <summary>
        /// Registra Hangfire con SQL Server y arranca el servidor con colas default/maintenance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Falta la cadena <c>ConnectionStrings:SqlServer</c>.</exception>
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            var hfConn = configuration.GetConnectionString("SqlServer")
                       ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer en appsettings.json");

            services.AddHangfire(cfg =>
            {
                cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                   .UseSimpleAssemblyNameTypeSerializer()
                   .UseRecommendedSerializerSettings()
                   .UseSqlServerStorage(
                       hfConn,
                       new SqlServerStorageOptions
                       {
                           SchemaName = configuration["Hangfire:Schema"] ?? "hangfire",
                           UseRecommendedIsolationLevel = true,
                           TryAutoDetectSchemaDependentOptions = true,
                           SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                           QueuePollInterval = TimeSpan.Zero // Polling inmediato
                       });
            });

            services.AddHangfireServer(options =>
            {
                options.Queues = new[] { "default", "maintenance" };
            });

            return services;
        }

        /// <summary>
        /// Añade el dashboard protegido y programa los jobs recurrentes configurados.
        /// </summary>
        public static IApplicationBuilder UseHangfireDashboardAndJobs(this IApplicationBuilder app, IConfiguration configuration)
        {
            var dashboardAuth = (Hangfire.Dashboard.IDashboardAuthorizationFilter)new HangfireDashboardAuth();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { dashboardAuth }
            });

            var tz = TZConvert.GetTimeZoneInfo(configuration["Hangfire:TimeZoneIana"] ?? "America/Bogota");

            // Job recurrente: generación mensual de obligaciones
            var cronObligations = configuration["Hangfire:CronObligations"] ?? "15 2 1 * *"; // Cada 1° del mes a las 2:15 AM
            RecurringJob.AddOrUpdate<ObligationJobs>(
                "obligations-monthly",
                j => j.GenerateForCurrentMonthAsync(JobCancellationToken.Null),
                cronObligations,
                new RecurringJobOptions { TimeZone = tz, QueueName = "maintenance" }
            );

            // Job recurrente: revisión periódica de contratos expirados
            if (configuration.GetValue<bool>("Contracts:Expiration:Enabled"))
            {
                var cronContracts = configuration["Contracts:Expiration:Cron"] ?? "*/10 * * * *"; // Cada 10 minutos
                RecurringJob.AddOrUpdate<ContractJobs>(
                    "contracts-expiration",
                    j => j.RunExpirationSweepAsync(),
                    cronContracts,
                    new RecurringJobOptions { TimeZone = tz, QueueName = "default" }
                );
            }

            return app;
        }
    }
}
