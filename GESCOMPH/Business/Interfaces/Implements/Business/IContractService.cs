using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Business.Interfaces.IBusiness;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;

namespace Business.Interfaces.Implements.Business
{
    /// <summary>
    /// Define las operaciones de negocio para la gestión de contratos,
    /// incluyendo su creación, consulta, expiración y obligaciones asociadas.
    /// </summary>
    public interface IContractService 
        : IBusiness<ContractSelectDto, ContractCreateDto, ContractUpdateDto>
    {
        /// <summary>
        /// Crea un contrato de forma transaccional, gestionando:
        /// - La creación o recuperación de la persona asociada.
        /// - La generación o reutilización del usuario.
        /// - La reserva de establecimientos vinculados.
        /// - La generación de obligaciones mensuales iniciales.
        /// - El envío del contrato en formato PDF por correo electrónico.
        /// </summary>
        /// <param name="dto">Datos del contrato a crear.</param>
        /// <returns>Identificador del contrato creado.</returns>
        Task<int> CreateContractWithPersonHandlingAsync(ContractCreateDto dto);

        /// <summary>
        /// Obtiene los contratos asociados al usuario autenticado.
        /// Si el usuario es administrador, retorna todos los contratos.
        /// </summary>
        /// <returns>Lista de contratos en formato de tarjeta.</returns>
        Task<IReadOnlyList<ContractCardDto>> GetMineAsync();

        /// <summary>
        /// Ejecuta el proceso de barrido de contratos expirados:
        /// - Marca contratos vencidos como inactivos.
        /// - Libera los establecimientos que tenían contratos expirados.
        /// </summary>
        /// <param name="ct">Token de cancelación opcional.</param>
        /// <returns>Resultado con el número de contratos desactivados y establecimientos liberados.</returns>
        Task<ExpirationSweepResult> RunExpirationSweepAsync(CancellationToken ct = default);

        /// <summary>
        /// Obtiene las obligaciones mensuales asociadas a un contrato específico.
        /// </summary>
        /// <param name="contractId">Identificador del contrato.</param>
        /// <returns>Lista de obligaciones mensuales asociadas al contrato.</returns>
        Task<IReadOnlyList<ObligationMonthSelectDto>> GetObligationsAsync(int contractId);
    }
}
