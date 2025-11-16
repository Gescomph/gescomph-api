using Data.Interfaz.IDataImplement.Business;
using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.Enum;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Business
{
    public class EstablishmentsRepository : DataGeneric<Establishment>, IEstablishmentsRepository
    {
        private readonly ApplicationDbContext _ctx;

        public EstablishmentsRepository(ApplicationDbContext context) : base(context)
        {
            _ctx = context;
        }

        private IQueryable<Establishment> BaseQuery()
        {
            return _dbSet.AsNoTracking()
                .Where(e => !e.IsDeleted &&
                            e.Plaza != null && e.Plaza.Active && !e.Plaza.IsDeleted)
                .Include(e => e.Plaza)
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id);
        }

        /// <summary>
        /// proyección polimórfica con imágenes
        /// </summary>
        private IQueryable<Establishment> SelectWithImages(IQueryable<Establishment> q)
        {
            return q.Select(e => new Establishment
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Address = e.Address,
                AreaM2 = e.AreaM2,
                RentValueBase = e.RentValueBase,
                Active = e.Active,
                UvtQty = e.UvtQty,
                PlazaId = e.PlazaId,
                Plaza = e.Plaza,

                Images = _ctx.Images
                    .Where(img => img.EntityType == EntityType.Establishment
                                  && img.EntityId == e.Id
                                  && img.Active
                                  && !img.IsDeleted)
                    .OrderBy(img => img.Id)
                    .ToList()
            });
        }

        private static bool IsEmpty(IReadOnlyCollection<int> ids) =>
            ids == null || ids.Count == 0;

        public override async Task<IEnumerable<Establishment>> GetAllAsync() =>
            await SelectWithImages(BaseQuery()).ToListAsync();

        public async Task<IEnumerable<Establishment>> GetAllAsync(ActivityFilter filter, int? limit = null)
        {
            var q = BaseQuery();

            if (filter == ActivityFilter.ActiveOnly)
                q = q.Where(e => e.Active);

            if (limit.HasValue && limit.Value > 0)
                q = q.Take(limit.Value);

            return await SelectWithImages(q).ToListAsync();
        }

        public async Task<IEnumerable<Establishment>> GetByPlazaIdAsync(int plazaId, ActivityFilter filter, int? limit = null)
        {
            var q = BaseQuery().Where(e => e.PlazaId == plazaId);

            if (filter == ActivityFilter.ActiveOnly)
                q = q.Where(e => e.Active);

            if (limit.HasValue && limit.Value > 0)
                q = q.Take(limit.Value);

            return await SelectWithImages(q).ToListAsync();
        }

        public async Task<Establishment?> GetByIdAnyAsync(int id)
        {
            var q = BaseQuery().Where(e => e.Id == id);
            return await SelectWithImages(q).FirstOrDefaultAsync();
        }

        public async Task<Establishment?> GetByIdActiveAsync(int id)
        {
            var q = BaseQuery().Where(e => e.Id == id && e.Active);
            return await SelectWithImages(q).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<EstablishmentBasicsDto>> GetBasicsByIdsAsync(IReadOnlyCollection<int> ids)
        {
            if (IsEmpty(ids)) return Array.Empty<EstablishmentBasicsDto>();

            return await _dbSet.AsNoTracking()
                .Where(e => ids.Contains(e.Id) && e.Active && !e.IsDeleted)
                .Select(e => new EstablishmentBasicsDto
                {
                    Id = e.Id,
                    RentValueBase = e.RentValueBase,
                    UvtQty = e.UvtQty
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<EstablishmentCardDto>> GetCardsAsync(ActivityFilter filter)
        {
            var q = BaseQuery();

            if (filter == ActivityFilter.ActiveOnly)
                q = q.Where(e => e.Active);

            return await q
                .Select(e => new EstablishmentCardDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    Address = e.Address,
                    AreaM2 = e.AreaM2,
                    RentValueBase = e.RentValueBase,
                    Active = e.Active,
                    PrimaryImagePath = _ctx.Images
                        .Where(img => img.EntityType == EntityType.Establishment &&
                                      img.EntityId == e.Id &&
                                      img.Active && !img.IsDeleted)
                        .OrderBy(img => img.Id)
                        .Select(img => img.FilePath)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }


        public async Task<IReadOnlyList<EstablishmentCardDto>> GetCardsByPlazaAsync(int plazaId, ActivityFilter filter)
        {
            var q = BaseQuery().Where(e => e.PlazaId == plazaId);

            if (filter == ActivityFilter.ActiveOnly)
                q = q.Where(e => e.Active);

            return await q
                .Select(e => new EstablishmentCardDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    Address = e.Address,
                    AreaM2 = e.AreaM2,
                    RentValueBase = e.RentValueBase,
                    Active = e.Active,
                    PrimaryImagePath = _ctx.Images
                        .Where(img => img.EntityType == EntityType.Establishment &&
                                      img.EntityId == e.Id &&
                                      img.Active && !img.IsDeleted)
                        .OrderBy(img => img.Id)
                        .Select(img => img.FilePath)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<int>> GetInactiveIdsAsync(IReadOnlyCollection<int> ids)
        {
            if (IsEmpty(ids)) return Array.Empty<int>();

            return await _dbSet.AsNoTracking()
                .Where(e => ids.Contains(e.Id) && !e.IsDeleted && !e.Active)
                .Select(e => e.Id)
                .ToListAsync();
        }

        public async Task<int> SetActiveByIdsAsync(IReadOnlyCollection<int> ids, bool active)
        {
            if (IsEmpty(ids)) return 0;

            return await _dbSet
                .Where(e => ids.Contains(e.Id) && !e.IsDeleted && e.Active != active)
                .ExecuteUpdateAsync(up => up.SetProperty(e => e.Active, _ => active));
        }

        public async Task<int> SetActiveByPlazaIdAsync(int plazaId, bool active) =>
            await _dbSet
                .Where(e => e.PlazaId == plazaId && !e.IsDeleted && e.Active != active)
                .ExecuteUpdateAsync(up => up.SetProperty(e => e.Active, _ => active));
    }
}
