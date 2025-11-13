using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.SecurityAuthentication;
using Data.Interfaz.IDataImplement.Business;
using Microsoft.Extensions.Logging;
using Utilities.Messaging.Implements;
using Utilities.Messaging.Interfaces;

namespace Business.Services.Business
{
    /// <summary>
    /// Servicio encargado de ejecutar las tareas de cobro prejurídico:
    /// - Envío de recordatorios antes del vencimiento.
    /// - Notificación de obligaciones vencidas.
    /// - Cálculo diario de mora.
    /// </summary>
    public class CollectionService : ICollectionService
    {
        private readonly IObligationMonthRepository _repo;
        private readonly IUserService _userService;
        private readonly ISendCode _emailService;
        private readonly ILogger<CollectionService> _log;

        private const decimal DailyLateRate = 0.00033m; // ~1% mensual

        public CollectionService(
            IObligationMonthRepository repo,
            IUserService userService,
            ISendCode emailService,
            ILogger<CollectionService> log)
        {
            _repo = repo;
            _userService = userService;
            _emailService = emailService;
            _log = log;
        }

        // ===========================================
        // 1️⃣ Aviso 8 días antes del vencimiento
        // ===========================================
        public async Task ProcessDueSoonNotificationsAsync(DateTime today, CancellationToken ct = default)
        {
            var dueSoonDate = today.AddDays(8);

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
                    if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    {
                        _log.LogWarning("No se encontró email para la persona {PersonId}", o.Contract.PersonId);
                        continue;
                    }

                    await _emailService.SendPaymentReminderAsync(
                        user.Email,
                        user.PersonName ?? "Arrendatario",
                        o.DueDate,
                        o.TotalAmount
                    );

                    o.NotifiedDueSoonAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error enviando aviso previo para obligación {Id}", o.Id);
                }
            }

            await _repo.UpdateManyAsync(obligations, ct);
            _log.LogInformation("Avisos previos enviados: {Count}", obligations.Count);
        }

        // ===========================================
        // 2️⃣ Notificación de mora (día siguiente)
        // ===========================================
        public async Task ProcessOverdueNotificationsAsync(DateTime today, CancellationToken ct = default)
        {
            var overdue = await _repo.GetOverdueUnnotifiedAsync(today, ct);
            if (!overdue.Any())
            {
                _log.LogInformation("No hay obligaciones vencidas sin notificar al {Date}", today);
                return;
            }

            foreach (var o in overdue)
            {
                try
                {
                    var user = await _userService.GetByPersonIdAsync(o.Contract.PersonId, ct);
                    if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    {
                        _log.LogWarning("No se encontró email para la persona {PersonId}", o.Contract.PersonId);
                        continue;
                    }

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

                    o.Status = "OVERDUE";
                    o.NotifiedOverdueAt = DateTime.UtcNow;
                    o.DaysLate = daysLate;
                    o.LateFeeAmount = lateAmount;
                    o.LateAmount = lateAmount;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error notificando obligación vencida {Id}", o.Id);
                }
            }

            await _repo.UpdateManyAsync(overdue, ct);
            _log.LogInformation("Notificaciones de mora enviadas: {Count}", overdue.Count);
        }

        // ===========================================
        // 3️⃣ Actualización de mora diaria
        // ===========================================
        public async Task UpdateLateFeesAsync(DateTime today, CancellationToken ct = default)
        {
            var overdue = await _repo.GetOverdueAsync(ct);
            if (!overdue.Any())
            {
                _log.LogInformation("No hay obligaciones en mora al {Date}", today);
                return;
            }

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
    }
}
