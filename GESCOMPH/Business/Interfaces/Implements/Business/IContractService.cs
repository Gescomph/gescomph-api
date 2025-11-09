using System.Collections.Generic;
using System.Threading.Tasks;
using Business.Interfaces.IBusiness;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;

namespace Business.Interfaces.Implements.Business
{
    /// <summary>
    /// Servicio de negocio para la gestión de contratos.
    /// Gestiona la creación de contratos, reserva de establecimientos,
    /// asociación de cláusulas, generación de obligaciones mensuales,
    /// y envío de notificaciones con el contrato en PDF.
    /// </summary>
    public interface IContractService
        : IBusiness<ContractSelectDto, ContractCreateDto, ContractUpdateDto>
    {
        /// <summary>
        /// Devuelve los contratos asociados al usuario actual.
        /// - Si el usuario es administrador, devuelve todos.
        /// - Si no, solo los contratos de la persona autenticada.
        /// </summary>
        Task<IEnumerable<ContractSelectDto>> GetMineAsync();

        /// <summary>
        /// Ejecuta el barrido de expiración de contratos.
        /// - Desactiva los contratos expirados.
        /// - Libera los establecimientos asociados.
        /// </summary>
        Task<ExpirationSweepResult> RunExpirationSweepAsync();

        /// <summary>
        /// Devuelve las obligaciones mensuales asociadas a un contrato.
        /// </summary>
        Task<IEnumerable<ObligationMonthSelectDto>> GetObligationsAsync(int contractId);

        Task<ContractPublicMetricsDto> GetMetricsAsync();
    }
}
