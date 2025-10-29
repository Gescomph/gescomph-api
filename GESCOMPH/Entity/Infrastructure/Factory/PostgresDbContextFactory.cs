using System;
using System.IO;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Entity.Infrastructure.Factory
{
    /// <summary>
    /// Fábrica usada por EF Core en tiempo de diseño para crear PostgresDbContext (Add-Migration, Update-Database).
    /// </summary>
    public sealed class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
    {
        public PostgresDbContext CreateDbContext(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // ============================
            // 🔍 1️⃣ Buscar appsettings.json
            // ============================
            var basePath = Directory.GetCurrentDirectory();

            // Caso 1: ejecutas Add-Migration desde Entity → subir hasta WebGESCOMPH/
            var candidate = Path.Combine(basePath, "..", "WebGESCOMPH", "appsettings.json");
            if (File.Exists(candidate))
                basePath = Path.GetFullPath(Path.Combine(basePath, "..", "WebGESCOMPH"));
            else if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
                throw new FileNotFoundException($"No se encontró appsettings.json ni en {basePath} ni en WebGESCOMPH/.");

            // ============================
            // 📖 2️⃣ Cargar configuración
            // ============================
            var cfg = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // ============================
            // 🔐 3️⃣ Obtener cadena conexión
            // ============================
            var conn = GetArg(args, "--connection") ?? cfg.GetConnectionString("Postgres")
                      ?? throw new InvalidOperationException("Falta ConnectionStrings:Postgres en appsettings.json");

            // ============================
            // 🧱 4️⃣ Configurar DbContext
            // ============================
            var opts = new DbContextOptionsBuilder<PostgresDbContext>()
                .UseNpgsql(conn, npg =>
                {
                    npg.MigrationsAssembly(typeof(PostgresDbContext).Assembly.FullName);
                    npg.EnableRetryOnFailure();
                })
                .Options;

            return new PostgresDbContext(opts);
        }

        private static string? GetArg(string[] args, string key)
        {
            var i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
