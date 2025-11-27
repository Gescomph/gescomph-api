using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces.Implements.Business
{
    public interface ICollectionService
    {
        /// <summary>
        /// Envía recordatorios de pago 8 días antes del vencimiento.
        /// </summary>
        Task ProcessDueSoonNotificationsAsync(DateTime today, CancellationToken ct = default);

        /// <summary>
        /// Notifica a los arrendatarios cuyas obligaciones han vencido.
        /// </summary>
        Task ProcessOverdueNotificationsAsync(DateTime today, CancellationToken ct = default);

        /// <summary>
        /// Actualiza los intereses de mora de todas las obligaciones vencidas.
        /// </summary>
        Task UpdateLateFeesAsync(DateTime today, CancellationToken ct = default);

        /// <summary>
        /// Actualiza las obligaciones de estado pediente a vencido cuando cumple la fecha
        /// </summary>
        Task ProcessMarkAsOverdueAsync(DateTime today, CancellationToken ct = default);

        /// <summary>
        /// Calcula cuantos dias de vencido tiene la obligacion
        /// </summary>
        Task CalculateLateDaysAsync(DateTime today, CancellationToken ct = default);

        /// <summary>
        /// Actualiza las obligaciones de estado de vencidas a Pre-juridicas
        /// </summary>
        Task ProcessPreJudicialNotificationsAsync(DateTime today, CancellationToken ct = default);

        /// <summary>
        /// Actualiza las obligaciones de estado de Pre-juridicas a Juridica
        /// </summary>
        Task ProcessJudicialNotificationsAsync(DateTime today, CancellationToken ct = default);
    }
}
