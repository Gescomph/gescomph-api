using Business.Interfaces.Implements.AdministrationSystem;
using Business.Interfaces.Implements.Business;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace WebGESCOMPH.RealTime.Collections
{
    /// <summary>
    /// Trabajos automáticos de cobro prejurídico, coactivo y jurídico.
    /// Ejecuta tareas en segundo plano mediante Hangfire.
    /// </summary>
    public sealed class CollectionJobs
    {
        private readonly ICollectionService _svc;
        private readonly ILogger<CollectionJobs> _log;
        private readonly IConfiguration _cfg;
        private readonly ICollectionSettingServices _sttg;


        public CollectionJobs(
            ICollectionService svc,
            ILogger<CollectionJobs> log,
            IConfiguration cfg,
            ICollectionSettingServices sttg)
        {
            _svc = svc;
            _log = log;
            _cfg = cfg;
            _sttg = sttg;
        }

        /// <summary>
        /// Ejecuta todos los procesos de cobro del día:
        /// - Avisos previos (8 días antes)
        /// - Notificaciones de mora
        /// - Cálculo de mora diaria
        /// </summary>
        [DisableConcurrentExecution(timeoutInSeconds: 60 * 30)]
        [AutomaticRetry(Attempts = 0)]
        public async Task RunDailyCollectionsAsync(IJobCancellationToken token)
        {
            token?.ThrowIfCancellationRequested();

            var tzId = _cfg["Hangfire:TimeZoneIana"] ?? "America/Bogota";
            var tz = TZConvert.GetTimeZoneInfo(tzId);
            var today = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            _log.LogInformation("🏦 Iniciando cobro automático para {Date}", today.ToShortDateString());

            // ============================
            // 1. Obtener todos los settings
            // ============================
            var settings = await _sttg.GetAllAsync();

            // ============================
            // 2. Convertir a diccionario
            // ============================
            var dict = settings.ToDictionary(s => s.Name, s => s);

            // ============================
            // 3. Recuperar parámetros
            // ============================
            var dueSoonDays = (int)dict["PreDebtNotice"].Value;
            var overdueDays = (int)dict["OverdueNoticeDays"].Value;

            _log.LogInformation("⏱ Aviso previo: {Days} días", dueSoonDays);
            _log.LogInformation("⚠ Notificación de mora: {Days} días", overdueDays);

            // ============================
            // 4. Ejecutar tareas con parámetros dinámicos
            // ============================
            await _svc.ProcessDueSoonNotificationsAsync(today.AddDays(-dueSoonDays), token.ShutdownToken);
            await _svc.ProcessOverdueNotificationsAsync(today.AddDays(-overdueDays), token.ShutdownToken);
            await _svc.UpdateLateFeesAsync(today, token.ShutdownToken);

            _log.LogInformation("✅ Finalizó proceso de cobro automático para {Date}", today);
        }


    }
}
