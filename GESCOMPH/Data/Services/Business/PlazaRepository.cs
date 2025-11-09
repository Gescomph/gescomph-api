using Data.Interfaz.IDataImplement.Business;
using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.Implements.Utilities;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.DTOs.Implements.Business.Plaza;
using Entity.Enum;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Business
{
    public class PlazaRepository : DataGeneric<Plaza>, IPlazaRepository
    {
        private readonly ApplicationDbContext _ctx;

        public PlazaRepository(ApplicationDbContext context) : base(context)
        {
            _ctx = context;
        }

        private IQueryable<Plaza> BaseQuery()
        {
            return _dbSet.AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id);
        }

        public async Task<IReadOnlyList<PlazaCardDto>> GetCardsAsync()
        {
            var q = BaseQuery();


            return await q
                .Select(e => new PlazaCardDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    Location = e.Location,
                    Active = e.Active,
                    PrimaryImagePath = _ctx.Images
                        .Where(img => img.EntityType == EntityType.Plaza &&
                                      img.EntityId == e.Id &&
                                      img.Active && !img.IsDeleted)
                        .OrderBy(img => img.Id)
                        .Select(img => img.FilePath)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        /// <summary>
        /// Retorna plaza + lista de imágenes polimórficas
        /// </summary>
        public async Task<List<(Plaza plaza, List<Images> images)>> GetAllWithImagesAsync()
        {
            var plazas = await BaseQuery().ToListAsync();

            var result = plazas
                .Select(p => (
                    plaza: p,
                    images: _ctx.Images
                        .Where(img => img.EntityType == EntityType.Plaza
                                   && img.EntityId == p.Id
                                   && img.Active && !img.IsDeleted)
                        .OrderBy(img => img.Id)
                        .ToList()
                ))
                .ToList();

            return result;
        }

        public async Task<(Plaza? plaza, List<Images> images)> GetByIdWithImagesAsync(int id)
        {
            var plaza = await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);

            if (plaza is null)
                return (null, new List<Images>());

            var images = await _ctx.Images
                .Where(img => img.EntityType == EntityType.Plaza
                           && img.EntityId == id
                           && img.Active && !img.IsDeleted)
                .OrderBy(img => img.Id)
                .ToListAsync();

            return (plaza, images);
        }
    }
}
