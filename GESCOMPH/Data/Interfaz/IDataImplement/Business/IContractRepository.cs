using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;

namespace Data.Interfaz.IDataImplement.Business
{
    public interface IContractRepository : IDataGeneric<Contract>
    {
        // Proyecciones para contratos (ya no usa DTOs)
        Task<IEnumerable<Contract>> GetByPersonAsync(int personId);

        // Operaciones por expiración
        Task<IEnumerable<int>> DeactivateExpiredAsync(DateTime utcNow);
        Task<int> ReleaseEstablishmentsForExpiredAsync(DateTime utcNow);

        Task<IEnumerable<Contract>> GetExpiringContractsAsync(DateTime fromUtc, DateTime toUtc);

        // Validación de negocio
        Task<bool> AnyActiveByPlazaAsync(int plazaId);

        // Creación de contrato con entidades
        Task<int> CreateContractAsync(Contract contract, IReadOnlyCollection<int> establishmentIds, IReadOnlyCollection<int>? clauseIds = null);


        Task<ContractPublicMetricsDto> GetPublicMetricsAsync();
    }
}
