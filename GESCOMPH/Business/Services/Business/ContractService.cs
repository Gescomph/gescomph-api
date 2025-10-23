using Business.CustomJWT;
using Business.Interfaces;
using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.Persons;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Interfaces.PDF;
using Business.Repository;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.DTOs.Implements.Persons.Person;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Messaging.Interfaces;

namespace Business.Services.Business
{
    /// <summary>
    /// Servicio encargado de gestionar la lógica de negocio asociada a los contratos,
    /// incluyendo la creación de personas y usuarios relacionados, validaciones,
    /// orquestación transaccional y acciones post-commit (correo, PDF, obligaciones).
    /// </summary>
    public class ContractService
        : BusinessGeneric<ContractSelectDto, ContractCreateDto, ContractUpdateDto, Contract>, IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IPersonService _personService;
        private readonly IEstablishmentService _establishmentService;
        private readonly IUserService _userService;
        private readonly ISendCode _emailService;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUser _user;
        private readonly IObligationMonthService _obligationMonthService;
        private readonly IUserContextService _userContextService;
        private readonly IContractPdfGeneratorService _contractPdfService;
        private readonly ILogger<ContractService> _logger;

        public ContractService(
            IContractRepository contractRepository,
            IPersonService personService,
            IEstablishmentService establishmentService,
            IUserService userService,
            IMapper mapper,
            ISendCode emailService,
            ICurrentUser user,
            IObligationMonthService obligationMonthService,
            IUserContextService userContextService,
            IContractPdfGeneratorService contractPdfService,
            IUnitOfWork uow,
            ILogger<ContractService> logger
        ) : base(contractRepository, mapper)
        {
            _contractRepository = contractRepository;
            _personService = personService;
            _establishmentService = establishmentService;
            _userService = userService;
            _emailService = emailService;
            _user = user;
            _obligationMonthService = obligationMonthService;
            _userContextService = userContextService;
            _contractPdfService = contractPdfService;
            _uow = uow;
            _logger = logger;
        }

        // ======================================================
        // =================== CREACIÓN PRINCIPAL ===============
        // ======================================================

        /// <summary>
        /// Crea un contrato de forma orquestada:
        /// - Valida el DTO.
        /// - Crea o recupera la persona.
        /// - Garantiza la existencia del usuario.
        /// - Genera el contrato y reserva establecimientos.
        /// - Agenda tareas post-commit (correo, PDF, obligaciones).
        /// Todo se ejecuta en una transacción (Unit of Work).
        /// </summary>
        public async Task<int> CreateContractWithPersonHandlingAsync(ContractCreateDto dto)
        {
            ValidatePayload(dto);

            try
            {
                var createdContract = await _uow.ExecuteAsync(async ct =>
                {
                    // 1. Creacion transaccional (persona, usuario, contrato)
                    var (contract, person, userResult) = await HandleContractCreationAsync(dto);

                    // 2. Acciones post-commit (correo, PDF, obligaciones)
                    await HandlePostCommitActionsAsync(dto, contract, person, userResult);

                    return contract;
                });

                return createdContract.Id;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear contrato para {Document}", dto.Document);
                throw new BusinessException("Ocurrió un error creando el contrato.", ex);
            }
        }

        // ======================================================
        // ==================== CONSULTAS =======================
        // ======================================================

        /// <summary>
        /// Retorna los contratos asociados al usuario autenticado.
        /// Si el usuario es administrador, devuelve todos los contratos.
        /// </summary>
        public async Task<IReadOnlyList<ContractCardDto>> GetMineAsync()
        {
            if (_user.EsAdministrador)
                return (await _contractRepository.GetCardsAllAsync()).ToList().AsReadOnly();

            if (!_user.PersonId.HasValue || _user.PersonId.Value <= 0)
                throw new BusinessException("El usuario autenticado no tiene persona asociada.");

            var contracts = await _contractRepository.GetCardsByPersonAsync(_user.PersonId.Value);
            return contracts.ToList().AsReadOnly();
        }

        /// <summary>
        /// Retorna las obligaciones mensuales asociadas a un contrato.
        /// </summary>
        public async Task<IReadOnlyList<ObligationMonthSelectDto>> GetObligationsAsync(int contractId)
        {
            if (contractId <= 0)
                throw new BusinessException("ContractId inválido.");

            return await _obligationMonthService.GetByContractAsync(contractId);
        }

        // ======================================================
        // ============== BARRIDO DE EXPIRACIÓN =================
        // ======================================================

        /// <summary>
        /// Realiza el barrido de expiración:
        /// - Desactiva contratos vencidos.
        /// - Libera establecimientos que ya no tienen contratos activos.
        /// </summary>
        public async Task<ExpirationSweepResult> RunExpirationSweepAsync(CancellationToken ct = default)
        {
            return await _uow.ExecuteAsync(async _ =>
            {
                var now = DateTime.UtcNow;
                var deactivated = await _contractRepository.DeactivateExpiredAsync(now);
                var released = await _contractRepository.ReleaseEstablishmentsForExpiredAsync(now);

                _uow.RegisterPostCommit(_ =>
                {
                    _logger.LogInformation(
                        "Barrido de expiración: {Count} contratos desactivados, {Estabs} establecimientos liberados.",
                        deactivated.Count, released);
                    return Task.CompletedTask;
                });

                return new ExpirationSweepResult(deactivated, released);
            }, ct);
        }

        // ======================================================
        // ================== MÉTODOS PRIVADOS ==================
        // ======================================================

        /// <summary>
        /// Orquesta la creación de persona, usuario y contrato dentro de una transacción.
        /// </summary>
        private async Task<(Contract contract, PersonSelectDto person, (int userId, bool created, string? tempPassword) userResult)>
            HandleContractCreationAsync(ContractCreateDto dto)
        {
            var personPayload = _mapper.Map<PersonDto>(dto);
            var person = await _personService.GetOrCreateByDocumentAsync(personPayload);

            var userResult = await _userService.EnsureUserForPersonAsync(person.Id, dto.Email);

            var (baseRent, uvtQty) = await _establishmentService.ReserveForContractAsync(dto.EstablishmentIds);

            var contract = _mapper.Map<Contract>(dto);
            contract.PersonId = person.Id;
            contract.TotalBaseRentAgreed = baseRent;
            contract.TotalUvtQtyAgreed = uvtQty;

            var establishmentIds = dto.EstablishmentIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (establishmentIds.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");

            contract.PremisesLeased = establishmentIds
                .Select(id => new PremisesLeased { EstablishmentId = id })
                .ToList();

            if (dto.ClauseIds is { Count: > 0 })
            {
                var clauseIds = dto.ClauseIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (clauseIds.Count > 0)
                {
                    contract.ContractClauses = clauseIds
                        .Select(id => new ContractClause { ClauseId = id })
                        .ToList();
                }
            }

            await _contractRepository.AddAsync(contract);

            return (contract, person, userResult);
        }

        /// <summary>
        /// Registra las acciones post-commit:
        /// - Envío de correo con credenciales (si aplica)
        /// - Generación de obligaciones mensuales
        /// - Envío del contrato en PDF
        /// </summary>
        private async Task HandlePostCommitActionsAsync(
            ContractCreateDto dto,
            Contract contract,
            PersonSelectDto person,
            (int userId, bool created, string? tempPassword) userResult)
        {
            var fullName = $"{person.FirstName} {person.LastName}".Trim();

            if (userResult.created &&
                !string.IsNullOrWhiteSpace(dto.Email) &&
                !string.IsNullOrWhiteSpace(userResult.tempPassword))
            {
                _userService.QueuePasswordEmail(dto.Email!, fullName, userResult.tempPassword!);
            }

            _uow.RegisterPostCommit(async _ =>
            {
                try
                {
                    var now = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.UtcNow,
                        TimeZoneConverter.TZConvert.GetTimeZoneInfo("America/Bogota"));

                    await _obligationMonthService.GenerateForContractMonthAsync(contract.Id, now.Year, now.Month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generando obligaciones para contrato {ContractId}", contract.Id);
                }
            });

            _uow.RegisterPostCommit(async _ =>
            {
                try
                {
                    var loaded = await _contractRepository.GetByIdAsync(contract.Id);
                    var snapshot = _mapper.Map<ContractSelectDto>(loaded ?? contract);
                    var pdf = await _contractPdfService.GeneratePdfAsync(snapshot);

                    if (!string.IsNullOrWhiteSpace(dto.Email))
                    {
                        await _emailService.SendContractWithPdfAsync(
                            dto.Email!,
                            fullName,
                            contract.Id.ToString("D6"),
                            pdf);
                    }

                    _logger.LogInformation("Contrato {ContractId} enviado a {Email}", contract.Id, dto.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando contrato {ContractId} a {Email}", contract.Id, dto.Email);
                }
            });
        }

        /// <summary>
        /// Valida reglas mínimas antes de iniciar la creación de contrato.
        /// </summary>
        private static void ValidatePayload(ContractCreateDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            if (dto.EstablishmentIds is null || dto.EstablishmentIds.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");
        }

        // ======================================================
        // ================ CONFIGURACIÓN BASE ==================
        // ======================================================

        protected override Expression<Func<Contract, string>>[] SearchableFields() =>
        [
            c => c.Person.FirstName,
            c => c.Person.LastName,
            c => c.Person.Document
        ];

        protected override string[] SortableFields() => new[]
        {
            nameof(Contract.StartDate),
            nameof(Contract.EndDate),
            nameof(Contract.TotalBaseRentAgreed),
            nameof(Contract.TotalUvtQtyAgreed),
            nameof(Contract.PersonId),
            nameof(Contract.Id),
            nameof(Contract.CreatedAt),
            nameof(Contract.Active)
        };

        protected override IDictionary<string, Func<string, Expression<Func<Contract, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Contract, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Contract.PersonId)] = value => entity => entity.PersonId == int.Parse(value),
                [nameof(Contract.Active)] = value => entity => entity.Active == bool.Parse(value),
                [nameof(Contract.StartDate)] = value => entity => entity.StartDate == DateTime.Parse(value),
                [nameof(Contract.EndDate)] = value => entity => entity.EndDate == DateTime.Parse(value)
            };
    }
}
