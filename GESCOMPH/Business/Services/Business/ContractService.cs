using Business.CustomJWT;
using Business.Interfaces;
using Business.Interfaces.Implements.AdministrationSystem;
using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.Persons;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Interfaces.Notifications;
using Business.Interfaces.PDF;
using Business.Repository;
using Data.Interfaz.IDataImplement.AdministrationSystem;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.DTOs.Implements.Persons.Person;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Entity.DTOs.Implements.Utilities;
using Entity.Enum;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Helpers.Business;
using Utilities.Messaging.Interfaces;

namespace Business.Services.Business
{
    public class ContractService
        : BusinessGeneric<ContractSelectDto, ContractCreateDto, ContractUpdateDto, Contract>, IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IPersonService _personService;
        private readonly IEstablishmentService _establishmentService;
        private readonly IAuthService _authService;
        private readonly ISendCode _emailService;
        private readonly ICurrentUser _user;
        private readonly IObligationMonthService _obligationMonthService;
        private readonly IUserContextService _userContextService;
        private readonly IContractPdfGeneratorService _contractPdfService;
        private readonly IContractNotificationService _contractNotificationService;
        private readonly INotificationService _notificationService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ContractService> _logger;
        private readonly IMapper _mapper;
        private const int ExpirationNotificationWindowDays = 5;
        private const string ContractActionRouteFormat = "/contracts/{0}";

        public ContractService(
            IContractRepository contractRepository,
            IPersonService personService,
            IEstablishmentService establishmentService,
            IAuthService authService,
            ISendCode emailService,
            ICurrentUser user,
            IObligationMonthService obligationMonthService,
            IUserContextService userContextService,
            IContractPdfGeneratorService contractPdfService,
            IContractNotificationService contractNotificationService,
            INotificationService notificationService,
            INotificationRepository notificationRepository,
            IUnitOfWork uow,
            ILogger<ContractService> logger,
            IMapper mapper
        ) : base(contractRepository, mapper)
        {
            _contractRepository = contractRepository;
            _personService = personService;
            _establishmentService = establishmentService;
            _authService = authService;
            _emailService = emailService;
            _user = user;
            _obligationMonthService = obligationMonthService;
            _userContextService = userContextService;
            _contractPdfService = contractPdfService;
            _contractNotificationService = contractNotificationService;
            _notificationService = notificationService;
            _notificationRepository = notificationRepository;
            _uow = uow;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtiene métricas públicas relacionadas con los contratos.
        /// </summary>
        public Task<ContractPublicMetricsDto> GetMetricsAsync()
                => _contractRepository.GetPublicMetricsAsync();

        /// <summary>
        /// Crea un nuevo contrato en el sistema, validando los datos, creando personas si es necesario,
        /// generando obligaciones, notificaciones y correos con el PDF del contrato.
        /// </summary>
        public override async Task<ContractSelectDto> CreateAsync(ContractCreateDto dto)
        {
            BusinessValidationHelper.ThrowIfNull(dto, "El DTO no puede ser nulo.");
            ValidatePayload(dto);

            Contract? createdContract = null;
            int personId = 0;

            await _uow.ExecuteAsync(async ct =>
            {
                var existingPerson = await _personService.GetByDocumentAsync(dto.Document);

                if (existingPerson == null)
                {
                    var registerDto = _mapper.Map<RegisterDto>(dto);
                    registerDto.Password = string.Empty;
                    var registered = await _authService.RegisterInternalAsync(registerDto);
                    personId = registered.PersonId ?? 0;
                }
                else
                {
                    personId = existingPerson.Id;
                }

                var (baseRent, uvtQty) = await _establishmentService.ReserveForContractAsync(dto.EstablishmentIds);

                var contract = BuildContract(dto, personId, baseRent, uvtQty);
                createdContract = await _contractRepository.AddAsync(contract);

                await _uow.SaveChangesAsync(ct);

                await SchedulePostCommitAsync(
                    createdContract.Id,
                    createdContract.PersonId,
                    createdContract.Person?.User?.Id,
                    dto.Email,
                    $"{dto.FirstName} {dto.LastName}".Trim(),
                    null
                );
            });

            createdContract = await _contractRepository.GetByIdAsync(createdContract!.Id)
                               ?? throw new BusinessException("No se pudo cargar el contrato recién creado.");

            return _mapper.Map<ContractSelectDto>(createdContract);
        }

        /// <summary>
        /// Ejecuta un barrido para desactivar contratos vencidos y liberar establecimientos asociados.
        /// </summary>
        public async Task<ExpirationSweepResult> RunExpirationSweepAsync()
        {
            var now = DateTime.UtcNow;
            await NotifyExpiringContractsAsync(now);

            return await _uow.ExecuteAsync(async ct =>
            {
                var deactivated = (await _contractRepository.DeactivateExpiredAsync(now)).ToList();
                var released = await _contractRepository.ReleaseEstablishmentsForExpiredAsync(now);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Barrido de expiración: {DeactivatedCount} contratos desactivados, {ReleasedCount} establecimientos liberados.",
                    deactivated.Count, released
                );

                return new ExpirationSweepResult(deactivated, released);
            });
        }

        private async Task NotifyExpiringContractsAsync(DateTime utcNow)
        {
            var windowEnd = utcNow.AddDays(ExpirationNotificationWindowDays);
            var since = utcNow.AddDays(-ExpirationNotificationWindowDays);
            var expiringContracts = (await _contractRepository.GetExpiringContractsAsync(utcNow, windowEnd)).ToList();

            if (expiringContracts.Count == 0)
            {
                return;
            }

            foreach (var contract in expiringContracts)
            {
                try
                {
                    var person = contract.Person;
                    var userId = person?.User?.Id ?? 0;
                    if (userId <= 0)
                    {
                        continue;
                    }

                    var actionRoute = string.Format(ContractActionRouteFormat, contract.Id);
                    var alreadyNotified = await _notificationRepository.HasRecentNotificationAsync(
                        userId,
                        NotificationType.ContractExpiring,
                        actionRoute,
                        since);

                    if (alreadyNotified)
                    {
                        continue;
                    }

                    var daysLeft = Math.Max(0, (contract.EndDate.Date - utcNow.Date).Days);
                    var dayText = daysLeft switch
                    {
                        <= 0 => "hoy",
                        1 => "en 1 día",
                        _ => $"en {daysLeft} días"
                    };

                    var personName = person is null
                        ? "inquilino"
                        : $"{person.FirstName} {person.LastName}".Trim();

                    var notificationDto = new NotificationCreateDto
                    {
                        Title = "Contrato por vencer",
                        Message = $"Hola {personName}, tu contrato #{contract.Id:D6} vence {dayText} ({contract.EndDate:dd/MM/yyyy}).",
                        Type = NotificationType.ContractExpiring,
                        Priority = NotificationPriority.Warning,
                        RecipientUserId = userId,
                        ActionRoute = actionRoute,
                        ExpiresAt = contract.EndDate
                    };

                    await _notificationService.CreateAsync(notificationDto);
                    await _contractNotificationService.NotifyContractExpiring(contract.Id, contract.PersonId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error enviando notificación de contrato próximo a vencer para {ContractId}.",
                        contract.Id);
                }
            }
        }

        /// <summary>
        /// Valida los datos del DTO del contrato antes de crearlo.
        /// </summary>
        private void ValidatePayload(ContractCreateDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            if (dto.EstablishmentIds is null || dto.EstablishmentIds.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");
        }

        /// <summary>
        /// Construye una entidad <see cref="Contract"/> a partir del DTO y otros parámetros.
        /// </summary>
        private Contract BuildContract(ContractCreateDto dto, int personId, decimal totalBaseRent, decimal totalUvt)
        {
            var contract = _mapper.Map<Contract>(dto);
            contract.PersonId = personId;
            contract.TotalBaseRentAgreed = totalBaseRent;
            contract.TotalUvtQtyAgreed = totalUvt;
            contract.Active = true;
            contract.PremisesLeased = dto.EstablishmentIds
                .Select(eid => new PremisesLeased { EstablishmentId = eid })
                .ToList();
            if (dto.ClauseIds is { Count: > 0 })
            {
                contract.ContractClauses = dto.ClauseIds
                    .Distinct()
                    .Select(cid => new ContractClause { ClauseId = cid })
                    .ToList();
            }

            return contract;
        }

        /// <summary>
        /// Crea una instantánea del contrato (DTO) para usos posteriores como notificaciones o PDF.
        /// </summary>
        private async Task<ContractSelectDto?> BuildSnapshotAsync(Contract contract)
        {
            var loaded = await _contractRepository.GetByIdAsync(contract.Id);
            return _mapper.Map<ContractSelectDto>(loaded ?? contract);
        }

        /// <summary>
        /// Compone el nombre completo de una persona.
        /// </summary>
        private string ComposeFullName(PersonSelectDto person)
            => $"{person.FirstName} {person.LastName}".Trim();

        /// <summary>
        /// Programa tareas posteriores al commit de la transacción, como generar obligaciones,
        /// enviar notificaciones y correos electrónicos con el contrato en PDF.
        /// </summary>
        private async Task SchedulePostCommitAsync(
            int contractId,
            int personId,
            int? recipientUserId,
            string? email,
            string fullName,
            ContractSelectDto? contractSnapshot)
        {
            _uow.RegisterPostCommit(async ct =>
            {
                try
                {
                    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneConverter.TZConvert.GetTimeZoneInfo("America/Bogota"));

                    await _obligationMonthService.GenerateForContractMonthAsync(contractId, now.Year, now.Month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generando obligaciones para contrato {ContractId}", contractId);
                }
            });

            if (personId > 0)
            {
                _uow.RegisterPostCommit(async ct =>
                {
                    try
                    {
                        await _contractNotificationService.NotifyContractCreated(contractId, personId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando notificación realtime {ContractId}", contractId);
                    }
                });
            }

            if (recipientUserId.HasValue && recipientUserId.Value > 0)
            {
                _uow.RegisterPostCommit(async ct =>
                {
                    try
                    {
                        var notificationDto = new NotificationCreateDto
                        {
                            Title = "Contrato creado",
                            Message = $"Hola {fullName}, tu contrato #{contractId:D6} fue creado correctamente.",
                            Type = NotificationType.ContractCreated,
                            Priority = NotificationPriority.Info,
                            RecipientUserId = recipientUserId.Value,
                            ActionRoute = "/contracts"
                        };

                        await _notificationService.CreateAsync(notificationDto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error creando notificación del sistema para contrato {ContractId}.",
                            contractId);
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                _uow.RegisterPostCommit(async ct =>
                {
                    try
                    {
                        var loaded = await _contractRepository.GetByIdAsync(contractId);
                        var snap = _mapper.Map<ContractSelectDto>(loaded!);

                        var pdf = await _contractPdfService.GeneratePdfAsync(snap);
                        await _emailService.SendContractWithPdfAsync(email!, fullName, contractId.ToString("D6"), pdf);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando correo PDF {ContractId}", contractId);
                    }
                });
            }
        }

        /// <summary>
        /// Obtiene los contratos asociados al usuario actual, o todos si es administrador.
        /// </summary>
        public async Task<IEnumerable<ContractSelectDto>> GetMineAsync()
        {
            IEnumerable<Contract> contracts;

            if (_user.EsAdministrador)
            {
                contracts = await _contractRepository.GetAllAsync();
            }
            else
            {
                if (!_user.PersonId.HasValue || _user.PersonId.Value <= 0)
                    throw new BusinessException("El usuario autenticado no tiene persona asociada. Complete el perfil antes de consultar contratos.");

                contracts = await _contractRepository.GetByPersonAsync(_user.PersonId.Value);
            }

            return _mapper.Map<IEnumerable<ContractSelectDto>>(contracts);
        }

        /// <summary>
        /// Obtiene las obligaciones mensuales asociadas a un contrato específico.
        /// </summary>
        public async Task<IEnumerable<ObligationMonthSelectDto>> GetObligationsAsync(int contractId)
        {
            if (contractId <= 0)
                throw new BusinessException("ContractId inválido.");

            return await _obligationMonthService.GetByContractAsync(contractId);
        }

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
