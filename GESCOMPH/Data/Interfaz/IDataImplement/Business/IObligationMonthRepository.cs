using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Business;

namespace Data.Interfaz.IDataImplement.Business
{
    public interface IObligationMonthRepository : IDataGeneric<ObligationMonth>
    {
        Task<ObligationMonth?> GetByContractYearMonthAsync(int contractId, int year, int month);
        IQueryable<ObligationMonth> GetByContractQueryable(int contractId);
        Task<decimal> GetTotalObligationsPaidByMonthAsync(int year, int month);
        Task<decimal> GetTotalObligationsPaidByDayAsync(DateTime date);
        Task<IEnumerable<object>> GetLastSixMonthsPaidAsync();

        /// <summary>
        /// Obtiene las obligaciones pendientes que vencerán en la fecha indicada y no han sido notificadas.
        /// </summary>
        Task<List<ObligationMonth>> GetPendingDueSoonAsync(DateTime dueDate, CancellationToken ct = default);

        /// <summary>
        /// Obtiene las obligaciones vencidas que aún no han recibido notificación de mora.
        /// </summary>
        Task<List<ObligationMonth>> GetOverdueUnnotifiedAsync(DateTime today, CancellationToken ct = default);

        /// <summary>
        /// Obtiene todas las obligaciones vencidas (en mora) activas y no pagadas.
        /// </summary>
        Task<List<ObligationMonth>> GetOverdueAsync(CancellationToken ct = default);

        /// <summary>
        /// Actualiza múltiples obligaciones en una sola operación (batch update).
        /// </summary>
        Task UpdateManyAsync(IEnumerable<ObligationMonth> obligations, CancellationToken ct = default);
    }
}
