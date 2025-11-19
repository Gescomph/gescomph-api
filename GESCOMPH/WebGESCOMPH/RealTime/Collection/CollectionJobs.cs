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

        public CollectionJobs(
            ICollectionService svc,
            ILogger<CollectionJobs> log,
            IConfiguration cfg)
        {
            _svc = svc;
            _log = log;
            _cfg = cfg;
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

            _log.LogInformation("🏦 Iniciando proceso de cobro automático para {Date}", today.ToShortDateString());

            // 1. Marcar como vencidas (PENDING -> OVERDUE)
            await _svc.ProcessMarkAsOverdueAsync(today, token.ShutdownToken);

            // 2. Calcular días de mora (Unidad de tiempo)
            await _svc.CalculateLateDaysAsync(today, token.ShutdownToken);

            // 3. Actualizar valor monetario de mora
            await _svc.UpdateLateFeesAsync(today, token.ShutdownToken);

            // --- NOTIFICACIONES & TRANSICIONES ---

            // 4. Aviso Previo (8 días antes)
            await _svc.ProcessDueSoonNotificationsAsync(today, token.ShutdownToken);

            // 5. Aviso de Mora (1 día después)
            await _svc.ProcessOverdueNotificationsAsync(today, token.ShutdownToken);

            // 6. NUEVO: Aviso Pre-Jurídico (30 días después)
            await _svc.ProcessPreJudicialNotificationsAsync(today, token.ShutdownToken);

            // 7. NUEVO: Aviso Jurídico (5 días después del pre-jurídico)
            await _svc.ProcessJudicialNotificationsAsync(today, token.ShutdownToken);


            _log.LogInformation("✅ Finalizó proceso de cobro automático {Date}", today);
        }
    }
}

