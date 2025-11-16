using Business.Interfaces.Implements.Business;
using Business.Repository;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.DTOs.Implements.Business.Plaza;
using Entity.Enum;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Utilities.Exceptions;

namespace Business.Services.Business
{
    public sealed class PlazaService :
        BusinessGeneric<PlazaSelectDto, PlazaCreateDto, PlazaUpdateDto, Plaza>, IPlazaService
    {
        private readonly IPlazaRepository _plazaRepository;
        private readonly IEstablishmentsRepository _establishmentsRepository;
        private readonly IContractRepository _contractRepository;
        private readonly ILogger<PlazaService> _logger;

        public PlazaService(
            IPlazaRepository plazaRepository,
            IMapper mapper,
            IEstablishmentsRepository establishmentsRepository,
            IContractRepository contractRepository,
            ILogger<PlazaService> logger
        ) : base(plazaRepository, mapper)
        {
            _plazaRepository = plazaRepository;
            _establishmentsRepository = establishmentsRepository;
            _contractRepository = contractRepository;
            _logger = logger;
        }

        // ---------------------------------------------
        // GET ALL / GET BY ID → con imágenes polimórficas
        // ---------------------------------------------


        // Lista liviana para grid/cards (sin Includes pesados)
        public async Task<IReadOnlyList<PlazaCardDto>> GetCardsAnyAsync()
        {
            var list = await _plazaRepository.GetCardsAsync();
            return list.ToList().AsReadOnly();
        }


        public override async Task<IEnumerable<PlazaSelectDto>> GetAllAsync()
        {
            var data = await _plazaRepository.GetAllWithImagesAsync();
            return data.Adapt<IEnumerable<PlazaSelectDto>>(); // gracias al mapping de Mapster
        }

        public override async Task<PlazaSelectDto?> GetByIdAsync(int id)
        {
            var data = await _plazaRepository.GetByIdWithImagesAsync(id);
            if (data.plaza is null) return null;

            return data.Adapt<PlazaSelectDto>(); // mapea plaza + images → dto
        }




        public async Task<PlazaSelectDto> CreateAsync(PlazaCreateDto dto)
        {

            // 1) map a entidad
            var entity = dto.Adapt<Plaza>();

            // 2) persistir
            await _plazaRepository.AddAsync(entity);

            // 3) cargar el registro completo desde DB (con el ID real ya generado)
            var created = await _plazaRepository.GetByIdAsync(entity.Id)
                          ?? entity;

            // 4) mapear a SELECT (este sí incluye id)
            return created.Adapt<PlazaSelectDto>();
        }

        // ---------------------------------------------
        // ACTIVATE / DEACTIVATE con validación dominio
        // ---------------------------------------------
        public override async Task UpdateActiveStatusAsync(int id, bool active)
        {
            var entity = await _plazaRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la plaza con ID {id}.");

            if (entity.Active == active)
                return;

            if (!active)
            {
                var hasActiveContracts = await _contractRepository.AnyActiveByPlazaAsync(id);
                if (hasActiveContracts)
                    throw new BusinessException("No se puede desactivar la plaza porque tiene contratos activos.");
            }

            entity.Active = active;
            await _plazaRepository.UpdateAsync(entity);

            // cascada
            await _establishmentsRepository.SetActiveByPlazaIdAsync(id, active);
        }

        // ---------------------------------------------
        // Uniqueness
        // ---------------------------------------------
        protected override IQueryable<Plaza>? ApplyUniquenessFilter(IQueryable<Plaza> query, Plaza candidate)
            => query.Where(p => p.Name == candidate.Name);

        // ---------------------------------------------
        // Search + Sorting
        // ---------------------------------------------
        protected override Expression<Func<Plaza, string>>[] SearchableFields() =>
        [
            p => p.Name,
            p => p.Description,
            p => p.Location
        ];

        protected override string[] SortableFields() =>
        [
            nameof(Plaza.Name),
            nameof(Plaza.Description),
            nameof(Plaza.Location),
            nameof(Plaza.Active),
            nameof(Plaza.CreatedAt),
            nameof(Plaza.Id)
        ];
    }
}
