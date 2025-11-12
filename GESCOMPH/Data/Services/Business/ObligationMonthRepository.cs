using Data.Interfaz.IDataImplement.Business;
using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Data.Services.Business
{
    public class ObligationMonthRepository : DataGeneric<ObligationMonth>, IObligationMonthRepository
    {
        public ObligationMonthRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ObligationMonth?> GetByContractYearMonthAsync(int contractId, int year, int month)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(o => o.ContractId == contractId && o.Year == year && o.Month == month && !o.IsDeleted);
        }

        public IQueryable<ObligationMonth> GetByContractQueryable(int contractId)
        {
            return _dbSet.AsNoTracking()
                         .Where(o => o.ContractId == contractId && !o.IsDeleted)
                         .OrderByDescending(o => o.Year)
                         .ThenByDescending(o => o.Month);
        }

        public async Task<decimal> GetTotalObligationsPaidByDayAsync(DateTime date)
        {
            return await _dbSet.AsNoTracking()
                              .Where(o => o.Status == "PAID"
                                  && o.PaymentDate.HasValue
                                  && o.PaymentDate.Value.Date == date.Date)
                              .SumAsync(o => o.TotalAmount);
        }

        /// <summary>
        /// Metodo que me carga el valor total pagado los
        /// ultimos meses apartir desde el vigente asi atras
        /// </summary>
        /// <returns>
        /// { "label": "May", "total": 2000000 }
        /// </returns>

        public async Task<IEnumerable<object>> GetLastSixMonthsPaidAsync()
        {
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var sixMonthsAgo = currentMonth.AddMonths(-5);

            var result = await _context.ObligationMonths
                .Where(o => o.Status == "PAID"
                            && o.PaymentDate != null
                            && o.PaymentDate >= sixMonthsAgo
                            && o.PaymentDate < currentMonth.AddMonths(1))
                .GroupBy(o => new { o.PaymentDate!.Value.Year, o.PaymentDate.Value.Month })
                .Select(g => new
                {
                    Label = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month),
                    Total = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            return result;
        }


        public async Task<decimal> GetTotalObligationsPaidByMonthAsync(int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            return await _dbSet.AsNoTracking()
                .Where(o => o.Status == "PAID"
                    && o.PaymentDate >= start
                    && o.PaymentDate < end)
                .SumAsync(o => o.TotalAmount);
        }

        /// <summary>
        /// Obligaciones pendientes que vencerán en la fecha indicada y aún no fueron notificadas (recordatorio previo).
        /// </summary>
        public async Task<List<ObligationMonth>> GetPendingDueSoonAsync(DateTime dueDate, CancellationToken ct = default)
        {
            return await _context.Set<ObligationMonth>()
                .Include(o => o.Contract)
                .AsNoTracking()
                .Where(o =>
                    o.Active &&
                    o.Status == "PENDING" &&
                    o.NotifiedDueSoonAt == null &&
                    o.DueDate.Date == dueDate.Date)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Obligaciones vencidas que aún no han recibido notificación de mora.
        /// </summary>
        public async Task<List<ObligationMonth>> GetOverdueUnnotifiedAsync(DateTime today, CancellationToken ct = default)
        {
            return await _context.Set<ObligationMonth>()
                .Include(o => o.Contract)
                .AsNoTracking()
                .Where(o =>
                    o.Active &&
                    o.Status == "PENDING" &&
                    o.PaymentDate == null &&
                    o.NotifiedOverdueAt == null &&
                    o.DueDate.Date < today.Date)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Obligaciones actualmente en mora (ya vencidas o marcadas como OVERDUE).
        /// </summary>
        public async Task<List<ObligationMonth>> GetOverdueAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;

            return await _context.Set<ObligationMonth>()
                .Include(o => o.Contract)
                .Where(o =>
                    o.Active &&
                    o.PaymentDate == null &&
                    (o.Status == "OVERDUE" || o.DueDate.Date < today))
                .ToListAsync(ct);
        }

        /// <summary>
        /// Actualiza múltiples obligaciones en una sola operación.
        /// </summary>
        public async Task UpdateManyAsync(IEnumerable<ObligationMonth> obligations, CancellationToken ct = default)
        {
            if (obligations == null || !obligations.Any()) return;

            foreach (var obligation in obligations)
            {
                _context.Entry(obligation).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
