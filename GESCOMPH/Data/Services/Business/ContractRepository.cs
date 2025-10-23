using Data.Interfaz.IDataImplement.Business;
using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Utilities.Exceptions;

namespace Data.Services.Business
{
    /// <summary>
    /// Repositorio encargado de gestionar la persistencia de contratos, 
    /// incluyendo creación, consultas y mantenimiento de consistencia con establecimientos y cláusulas.
    /// </summary>
    public class ContractRepository : DataGeneric<Contract>, IContractRepository
    {
        public ContractRepository(ApplicationDbContext context) : base(context) { }

        // ================== QUERIES BASE ==================

        /// <summary>
        /// Construye la query completa de contratos incluyendo persona, usuario, locales y cláusulas.
        /// </summary>
        private IQueryable<Contract> GetContractFullQuery()
        {
            return _dbSet
                .Include(c => c.Person).ThenInclude(p => p.User)
                .Include(c => c.PremisesLeased).ThenInclude(pl => pl.Establishment).ThenInclude(e => e.Plaza)
                .Include(c => c.ContractClauses).ThenInclude(cc => cc.Clause)
                .AsNoTracking();
        }

        /// <summary>
        /// Construye la query para tarjetas de contrato (resumen), con filtro opcional por persona.
        /// </summary>
        private IQueryable<ContractCardDto> GetCardQuery(int? personId = null)
        {
            var query = _dbSet.AsNoTracking()
                .Where(c => !c.IsDeleted);

            if (personId.HasValue)
                query = query.Where(c => c.PersonId == personId.Value);

            return query
                .OrderByDescending(e => e.Active)
                .ThenByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id)
                .Select(c => new ContractCardDto(
                    c.Id,
                    c.PersonId,
                    c.Person.FirstName,
                    c.Person.LastName,
                    c.Person.Document,
                    c.Person.Phone,
                    c.Person.User != null ? c.Person.User.Email : null,
                    c.StartDate,
                    c.EndDate,
                    c.TotalBaseRentAgreed,
                    c.TotalUvtQtyAgreed,
                    c.Active
                ));
        }

        // ================== MÉTODOS ==================

        /// <summary>
        /// Obtiene un contrato completo (con relaciones) por su identificador.
        /// </summary>
        public override async Task<Contract?> GetByIdAsync(int id) =>
            await GetContractFullQuery().FirstOrDefaultAsync(c => c.Id == id);

        /// <summary>
        /// Obtiene todas las tarjetas de contrato asociadas a una persona.
        /// </summary>
        public async Task<IReadOnlyList<ContractCardDto>> GetCardsByPersonAsync(int personId) =>
            await GetCardQuery(personId).ToListAsync();

        /// <summary>
        /// Obtiene todas las tarjetas de contrato disponibles (sin filtro por persona).
        /// </summary>
        public async Task<IReadOnlyList<ContractCardDto>> GetCardsAllAsync() =>
            await GetCardQuery().ToListAsync();

        /// <summary>
        /// Sobrescribe el método genérico AddAsync para crear un contrato completo 
        /// dentro del contexto de una unidad de trabajo (sin SaveChanges).
        /// Incluye validaciones, cálculo de totales, vínculos de cláusulas y actualización de locales.
        /// </summary>
        public override async Task<Contract> AddAsync(Contract contract)
        {
            ValidateContract(contract);

            var establishmentIds = GetEstablishmentIds(contract);

            await SetTotalsAsync(contract, establishmentIds);
            await AddContractClausesAsync(contract);
            await DeactivateEstablishmentsAsync(establishmentIds);

            await _dbSet.AddAsync(contract);

            return contract; // SaveChanges lo realiza el UnitOfWork
        }

        // ================== MÉTODOS PRIVADOS (modulares) ==================

        /// <summary>
        /// Valida reglas básicas de negocio antes de persistir el contrato.
        /// </summary>
        private static void ValidateContract(Contract contract)
        {
            if (contract.PersonId <= 0)
                throw new BusinessException("El contrato debe estar asociado a una persona válida.");

            if (contract.PremisesLeased == null || contract.PremisesLeased.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");
        }

        /// <summary>
        /// Extrae los identificadores únicos de establecimientos asociados al contrato.
        /// </summary>
        private static List<int> GetEstablishmentIds(Contract contract)
        {
            return contract.PremisesLeased
                .Select(pl => pl.EstablishmentId)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Calcula y asigna los totales de renta y UVT a partir de los establecimientos vinculados.
        /// </summary>
        private async Task SetTotalsAsync(Contract contract, IReadOnlyCollection<int> establishmentIds)
        {
            var basics = await _context.Set<Establishment>()
                .AsNoTracking()
                .Where(e => establishmentIds.Contains(e.Id))
                .Select(e => new { e.RentValueBase, e.UvtQty })
                .ToListAsync();

            contract.TotalBaseRentAgreed = basics.Sum(b => b.RentValueBase);
            contract.TotalUvtQtyAgreed = basics.Sum(b => b.UvtQty);
        }

        /// <summary>
        /// Crea las relaciones de cláusulas asociadas al contrato.
        /// Si el contrato no tiene cláusulas, no realiza acción alguna.
        /// </summary>
        private async Task AddContractClausesAsync(Contract contract)
        {
            if (contract.ContractClauses == null || !contract.ContractClauses.Any())
                return;

            var uniqueClauseIds = contract.ContractClauses
                .Select(cc => cc.ClauseId)
                .Distinct()
                .ToList();

            var links = uniqueClauseIds.Select(cid => new ContractClause
            {
                Contract = contract,
                ClauseId = cid
            });

            await _context.ContractClauses.AddRangeAsync(links);
        }

        /// <summary>
        /// Desactiva los establecimientos que fueron asignados al nuevo contrato.
        /// </summary>
        private async Task DeactivateEstablishmentsAsync(IReadOnlyCollection<int> establishmentIds)
        {
            if (establishmentIds.Count == 0) return;

            await _context.Set<Establishment>()
                .Where(e => establishmentIds.Contains(e.Id))
                .ExecuteUpdateAsync(up => up.SetProperty(e => e.Active, _ => false));
        }

        // ================== OTRAS OPERACIONES ==================

        /// <summary>
        /// Desactiva todos los contratos cuya fecha de finalización ya haya expirado.
        /// </summary>
        public async Task<IReadOnlyList<int>> DeactivateExpiredAsync(DateTime utcNow)
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

        /// <summary>
        /// Reactiva los establecimientos cuyos contratos expiraron y que no estén ocupados por otro contrato activo.
        /// </summary>
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

        /// <summary>
        /// Indica si existen contratos activos en una plaza específica.
        /// </summary>
        public async Task<bool> AnyActiveByPlazaAsync(int plazaId)
        {
            return await _dbSet.AsNoTracking()
                .Where(c => !c.IsDeleted && c.Active)
                .SelectMany(c => c.PremisesLeased)
                .AnyAsync(pl => !pl.IsDeleted &&
                                !pl.Establishment.IsDeleted &&
                                pl.Establishment.PlazaId == plazaId);
        }
    }
}
