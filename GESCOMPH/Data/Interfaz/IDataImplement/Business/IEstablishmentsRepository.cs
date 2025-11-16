using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.Enum;

namespace Data.Interfaz.IDataImplement.Business
{
    public interface IEstablishmentsRepository : IDataGeneric<Establishment>
    {
        // Listado completo — ahora devuelve DTO con imágenes
        Task<IEnumerable<EstablishmentSelectDto>> GetAllAsync(ActivityFilter filter, int? limit = null);

        // Consulta por plaza — devuelve DTO con imágenes
        Task<IEnumerable<EstablishmentSelectDto>> GetByPlazaIdAsync(int plazaId, ActivityFilter filter, int? limit = null);

        // Detalles — devuelve DTO con imágenes
        Task<EstablishmentSelectDto?> GetByIdAnyAsync(int id);
        Task<EstablishmentSelectDto?> GetByIdActiveAsync(int id);

        // Proyección liviana (solo valores necesarios)
        Task<IReadOnlyList<EstablishmentBasicsDto>> GetBasicsByIdsAsync(IReadOnlyCollection<int> ids);

        // Proyección optimizada para tarjetas
        Task<IReadOnlyList<EstablishmentCardDto>> GetCardsAsync(ActivityFilter filter);
        Task<IReadOnlyList<EstablishmentCardDto>> GetCardsByPlazaAsync(int plazaId, ActivityFilter filter);

        // Validación
        Task<IReadOnlyList<int>> GetInactiveIdsAsync(IReadOnlyCollection<int> ids);

        // Comandos
        Task<int> SetActiveByIdsAsync(IReadOnlyCollection<int> ids, bool active);
        Task<int> SetActiveByPlazaIdAsync(int plazaId, bool active);
    }
}
