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
    }
}
