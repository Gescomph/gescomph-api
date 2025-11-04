using Data.Interfaz.IDataImplement.Business;
using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Utilities.Exceptions;

namespace Data.Services.Business
{
    public class ContractRepository : DataGeneric<Contract>, IContractRepository
    {
        public ContractRepository(ApplicationDbContext context) : base(context) { }

        // ================== QUERIES BASE ==================

        private IQueryable<Contract> GetContractFullQuery()
        {
            return _dbSet
                .Include(c => c.Person).ThenInclude(p => p.User)
                .Include(c => c.PremisesLeased).ThenInclude(pl => pl.Establishment).ThenInclude(e => e.Plaza)
                .Include(c => c.ContractClauses).ThenInclude(cc => cc.Clause)
                .AsNoTracking();
        }

        // ================== MÉTODOS ==================

        public override async Task<Contract?> GetByIdAsync(int id) =>
            await GetContractFullQuery().FirstOrDefaultAsync(c => c.Id == id);

        public async Task<IEnumerable<Contract>> GetByPersonAsync(int personId)
        {
            return await GetContractFullQuery()
                .Where(c => !c.IsDeleted && c.PersonId == personId)
                .OrderByDescending(c => c.Active)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public override async Task<IEnumerable<Contract>> GetAllAsync()
        {
            return await GetContractFullQuery()
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.Active)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CreateContractAsync(Contract contract, IReadOnlyCollection<int> establishmentIds, IReadOnlyCollection<int>? clauseIds = null)
        {
            if (establishmentIds == null || establishmentIds.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");

            // Totales calculados directamente desde los establecimientos
            var basics = await _context.Set<Establishment>()
                .AsNoTracking()
                .Where(e => establishmentIds.Contains(e.Id))
                .Select(e => new { e.RentValueBase, e.UvtQty })
                .ToListAsync();

            contract.TotalBaseRentAgreed = basics.Sum(b => b.RentValueBase);
            contract.TotalUvtQtyAgreed = basics.Sum(b => b.UvtQty);
            contract.Active = true;

            foreach (var estId in establishmentIds)
                contract.PremisesLeased.Add(new PremisesLeased { EstablishmentId = estId });

            await _dbSet.AddAsync(contract);

            if (clauseIds is { Count: > 0 })
            {
                var uniqueClauseIds = clauseIds.Distinct().ToList();
                var links = uniqueClauseIds.Select(cid => new ContractClause
                {
                    Contract = contract,
                    ClauseId = cid
                });
                await _context.ContractClauses.AddRangeAsync(links);
            }

            await _context.Set<Establishment>()
                .Where(e => establishmentIds.Contains(e.Id))
                .ExecuteUpdateAsync(up => up.SetProperty(e => e.Active, _ => false));

            await _context.SaveChangesAsync();
            return contract.Id;
        }

        public async Task<IEnumerable<int>> DeactivateExpiredAsync(DateTime utcNow)
        {
            var ids = await _dbSet
                .Where(c => !c.IsDeleted && c.Active && c.EndDate < utcNow)
                .Select(c => c.Id)
                .ToListAsync();

            if (ids.Count == 0) return ids;

            await _dbSet
                .Where(c => ids.Contains(c.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Active, false));

            return ids;
        }

        public async Task<int> ReleaseEstablishmentsForExpiredAsync(DateTime utcNow)
        {
            var expiredIds = await _dbSet
                .Where(c => !c.IsDeleted && !c.Active && c.EndDate < utcNow)
                .Select(c => c.Id)
                .ToListAsync();

            if (expiredIds.Count == 0) return 0;

            var estIds = await _context.Set<PremisesLeased>()
                .Where(p => expiredIds.Contains(p.ContractId))
                .Select(p => p.EstablishmentId)
                .Distinct()
                .ToListAsync();

            if (estIds.Count == 0) return 0;

            var estToActivate = await _context.Set<PremisesLeased>()
                .Where(pl => estIds.Contains(pl.EstablishmentId))
                .GroupBy(pl => pl.EstablishmentId)
                .Where(g => !g.Any(pl =>
                    _dbSet.Any(c => !c.IsDeleted && c.Id == pl.ContractId && c.Active)))
                .Select(g => g.Key)
                .ToListAsync();

            if (estToActivate.Count == 0) return 0;

            return await _context.Set<Establishment>()
                .Where(e => estToActivate.Contains(e.Id) && !e.Active)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Active, true));
        }

        public async Task<bool> AnyActiveByPlazaAsync(int plazaId)
        {
            return await _dbSet.AsNoTracking()
                .Where(c => !c.IsDeleted && c.Active)
                .SelectMany(c => c.PremisesLeased)
                .AnyAsync(pl => !pl.IsDeleted &&
                                !pl.Establishment.IsDeleted &&
                                pl.Establishment.PlazaId == plazaId);
        }



        public async Task<ContractPublicMetricsDto> GetPublicMetricsAsync()
        {
            var total = await _context.Contracts.CountAsync();
            var activos = await _context.Contracts.CountAsync(x => x.Active);
            var inactivos = total - activos;

            return new ContractPublicMetricsDto
            {
                Total = total,
                Activos = activos,
                Inactivos = inactivos
            };
        }
    }
}
