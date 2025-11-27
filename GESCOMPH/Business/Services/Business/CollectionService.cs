using Business.Interfaces.Implements.AdministrationSystem;
using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.SecurityAuthentication;
using Data.Interfaz.IDataImplement.Business;
using Entity.DTOs.Implements.AdministrationSystem.CollectionSetting;
using Entity.Enum;
using Microsoft.Extensions.Logging;
using Utilities.Messaging.Interfaces;

namespace Business.Services.Business
{
    public class CollectionService : ICollectionService
    {
        private readonly IObligationMonthRepository _repo;
        private readonly IUserService _userService;
        private readonly ISendCode _emailService;
        private readonly ICollectionSettingServices _settingSvc;
        private readonly ILogger<CollectionService> _log;

        private const decimal DailyLateRate = 0.00033m; // ~1% mensual

        public CollectionService(
            IObligationMonthRepository repo,
            IUserService userService,
            ISendCode emailService,
            ICollectionSettingServices settingSvc,
            ILogger<CollectionService> log)
        {
            _repo = repo;
            _userService = userService;
            _emailService = emailService;
            _settingSvc = settingSvc;
            _log = log;
        }

        private async Task<Dictionary<string, CollectionSettingSelectDto>> GetSettingsAsync()
        {
            var settings = await _settingSvc.GetAllAsync();
            return settings.ToDictionary(s => s.Name, s => s);
        }

        // ===========================================
        // 1️⃣ AVISO PREVIO (Antes de vencer)
        // ===========================================
        public async Task ProcessDueSoonNotificationsAsync(DateTime today, CancellationToken ct = default)
        {
            var cfg = await GetSettingsAsync();
            var preDue = cfg.ContainsKey("PreDueDays") ? cfg["PreDueDays"].TimeSpan : TimeSpan.FromDays(8);
            var dueSoonDate = today.Add(preDue);

            var obligations = await _repo.GetPendingDueSoonAsync(dueSoonDate, ct);

            if (!obligations.Any())
            {
                _log.LogInformation("No hay obligaciones para aviso previo {Date}", dueSoonDate);
                return;
            }

            foreach (var o in obligations)
            {
                try
                {
                    var user = await _userService.GetByPersonIdAsync(o.Contract.PersonId, ct);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        await _emailService.SendPaymentReminderAsync(user.Email, user.PersonName ?? "Arrendatario", o.DueDate, o.TotalAmount);
                        o.NotifiedDueSoonAt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error enviando aviso previo Id {Id}", o.Id);
                }
            }
            await _repo.UpdateManyAsync(obligations, ct);
            _log.LogInformation("Avisos previos enviados: {Count}", obligations.Count);
        }

        // ===========================================
        // 2️⃣ MARCAR COMO VENCIDAS + NOTIFICACIÓN INMEDIATA
        // ===========================================
        public async Task ProcessMarkAsOverdueAsync(DateTime today, CancellationToken ct = default)
        {
            // Busca obligaciones PENDING cuya fecha de vencimiento ya pasó (ayer o antes)
            var toMark = await _repo.GetPendingExpiredAsync(today, ct);

            if (!toMark.Any())
            {
                _log.LogInformation("No hay obligaciones para marcar como vencidas al {Date}", today);
                return;
            }

            foreach (var o in toMark)
            {
                try
                {
                    // 1. Cambio de Estado y Cálculo Inicial
                    o.Status = Status.Vencida;
                    o.DaysLate = (today - o.DueDate.Date).Days;
                    if (o.DaysLate < 1) o.DaysLate = 1;

                    o.LateFeeAmount = o.TotalAmount * DailyLateRate * o.DaysLate;
                    o.LateAmount = o.LateFeeAmount;

                    // 2. Envío de Correo (Reutilizamos SendOverdueNoticeAsync)
                    var user = await _userService.GetByPersonIdAsync(o.Contract.PersonId, ct);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        await _emailService.SendOverdueNoticeAsync(
                            user.Email,
                            user.PersonName ?? "Arrendatario",
                            o.DueDate,
                            o.TotalAmount,
                            o.DaysLate.Value,
                            o.LateFeeAmount.Value
                        );

                        o.NotifiedOverdueAt = DateTime.UtcNow;
                        _log.LogInformation("📧 Notificación de vencimiento enviada a {Email} (Ob: {Id})", user.Email, o.Id);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error procesando vencimiento para obligación {Id}", o.Id);
                }
            }

            await _repo.UpdateManyAsync(toMark, ct);
            _log.LogInformation("Obligaciones marcadas como vencidas: {Count}", toMark.Count);
        }

        // ===========================================
        // 3️⃣ RECORDATORIO DE MORA (Respaldo para días siguientes)
        // ===========================================
        public async Task ProcessOverdueNotificationsAsync(DateTime today, CancellationToken ct = default)
        {
            var cfg = await GetSettingsAsync();
            var overdueOffset = cfg.ContainsKey("OverdueDays") ? cfg["OverdueDays"].TimeSpan : TimeSpan.FromDays(1);
            var overdueCheckDate = today.Add(-overdueOffset);

            // Busca obligaciones vencidas que NO han sido notificadas de mora aún
            // (Útil si el proceso del paso 2 falló en el envío del correo o si hay lógica de reintento)
            var overdue = await _repo.GetOverdueUnnotifiedAsync(overdueCheckDate, ct);

            if (!overdue.Any()) return;

            foreach (var o in overdue)
            {
                try
                {
                    var user = await _userService.GetByPersonIdAsync(o.Contract.PersonId, ct);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        var daysLate = (today - o.DueDate.Date).Days;
                        if (daysLate < 1) daysLate = 1;
                        var lateAmount = o.TotalAmount * DailyLateRate * daysLate;

                        await _emailService.SendOverdueNoticeAsync(
                            user.Email,
                            user.PersonName ?? "Arrendatario",
                            o.DueDate,
                            o.TotalAmount,
                            daysLate,
                            lateAmount
                        );

                        o.NotifiedOverdueAt = DateTime.UtcNow;

                        // Asegurar valores por si acaso
                        o.Status = Status.Vencida;
                        o.DaysLate = daysLate;
                        o.LateFeeAmount = lateAmount;
                        o.LateAmount = lateAmount;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error en recordatorio de mora Id {Id}", o.Id);
                }
            }
            await _repo.UpdateManyAsync(overdue, ct);
        }

        // ===========================================
        // 4️⃣ ACTUALIZACIÓN DIARIA DE MORA ($$)
        // ===========================================
        public async Task UpdateLateFeesAsync(DateTime today, CancellationToken ct = default)
        {
            var overdue = await _repo.GetOverdueAsync(ct);
            if (!overdue.Any()) return;

            foreach (var o in overdue)
            {
                var daysLate = (today - o.DueDate.Date).Days;
                if (daysLate < 1) continue;

                o.DaysLate = daysLate;
                o.LateFeeAmount = o.TotalAmount * DailyLateRate * daysLate;
                o.LateAmount = o.LateFeeAmount;
            }
            await _repo.UpdateManyAsync(overdue, ct);
            _log.LogInformation("Mora actualizada para {Count} obligaciones", overdue.Count);
        }

        // ===========================================
        // 5️⃣ CÁLCULO DE DÍAS (Unidad de Tiempo)
        // ===========================================
        public async Task CalculateLateDaysAsync(DateTime today, CancellationToken ct = default)
        {
            var cfg = await GetSettingsAsync();
            var offset = cfg.ContainsKey("LateFeeDayUnit") ? cfg["LateFeeDayUnit"].TimeSpan : TimeSpan.FromDays(1);
            var secondsPerDay = offset.TotalSeconds > 0 ? offset.TotalSeconds : 86400;

            var obligations = await _repo.GetOverdueOrPendingAsync(today);
            if (!obligations.Any()) return;

            foreach (var o in obligations)
            {
                var elapsed = today - o.DueDate;
                if (elapsed.TotalSeconds <= 0) continue;

                var DaysLate = (int)(elapsed.TotalSeconds / secondsPerDay);
                if (DaysLate < 1) DaysLate = 1;

                o.DaysLate = DaysLate;
                o.LateFeeAmount = o.TotalAmount * DailyLateRate;
                o.LateAmount = o.LateFeeAmount * o.DaysLate;
            }
            await _repo.UpdateManyAsync(obligations, ct);
        }

        // ===========================================
        // 6️⃣ COBRO PRE-JURÍDICO (30 días de mora)
        // ===========================================
        public async Task ProcessPreJudicialNotificationsAsync(DateTime today, CancellationToken ct = default)
        {
            var cfg = await GetSettingsAsync();

            // Días necesarios para pasar a esta etapa (Default: 30)
            var daysConfig = cfg.ContainsKey("PreJudicialDays") ? (int)cfg["PreJudicialDays"].Value : 30;
            var limitDate = today.AddDays(-daysConfig);

            var obligations = await _repo.GetOverdueForPreJudicialAsync(limitDate, ct);
            if (!obligations.Any()) return;

            // Días de plazo que se otorgarán (Default: 5)
            var graceDays = cfg.ContainsKey("JudicialGraceDays") ? (int)cfg["JudicialGraceDays"].Value : 5;
            var paymentDeadline = today.AddDays(graceDays);

            foreach (var o in obligations)
            {
                try
                {
                    var user = await _userService.GetByPersonIdAsync(o.Contract.PersonId, ct);
                    if (user == null || string.IsNullOrWhiteSpace(user.Email)) continue;

                    var totalDebt = o.TotalAmount + (o.LateAmount ?? 0);

                    // Enviar correo específico de Pre-Jurídico
                    await _emailService.SendPreJudicialNoticeAsync(
                        user.Email,
                        user.PersonName ?? "Arrendatario",
                        totalDebt,
                        paymentDeadline
                    );

                    o.Status = Status.PreJuridico;
                    o.NotifiedPreJudicialAt = DateTime.UtcNow;

                    _log.LogInformation("📧 [PRE-JURÍDICO] Enviado a {Email}. Deuda: {Debt}", user.Email, totalDebt);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error procesando cobro pre-jurídico Id: {Id}", o.Id);
                }
            }
            await _repo.UpdateManyAsync(obligations, ct);
        }

        // ===========================================
        // 7️⃣ COBRO JURÍDICO (Fin del plazo de gracia)
        // ===========================================
        public async Task ProcessJudicialNotificationsAsync(DateTime today, CancellationToken ct = default)
        {
            var cfg = await GetSettingsAsync();
            var graceDays = cfg.ContainsKey("JudicialGraceDays") ? (int)cfg["JudicialGraceDays"].Value : 5;

            // Buscamos obligaciones que llevan en Pre-Jurídico más tiempo del permitido
            var limitDate = today.AddDays(-graceDays);

            var obligations = await _repo.GetPreJudicialForJudicialAsync(limitDate, ct);
            if (!obligations.Any()) return;

            foreach (var o in obligations)
            {
                try
                {
                    var user = await _userService.GetByPersonIdAsync(o.Contract.PersonId, ct);
                    if (user == null || string.IsNullOrWhiteSpace(user.Email)) continue;

                    // Enviar correo final Jurídico
                    await _emailService.SendJudicialNoticeAsync(
                        user.Email,
                        user.PersonName ?? "Arrendatario"
                    );

                    o.Status = Status.Juridico;
                    o.NotifiedJudicialAt = DateTime.UtcNow;

                    _log.LogInformation("⚖️ [JURÍDICO] Notificación final enviada a {Email}.", user.Email);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error procesando cobro jurídico Id: {Id}", o.Id);
                }
            }
            await _repo.UpdateManyAsync(obligations, ct);
            _log.LogInformation("🚨 {Count} obligaciones pasaron a estado JUDICIAL.", obligations.Count);
        }
    }
}