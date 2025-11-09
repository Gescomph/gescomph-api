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
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
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
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ContractService> _logger;
        private readonly IMapper _mapper;

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
            _uow = uow;
            _logger = logger;
            _mapper = mapper;
        }





        public Task<ContractPublicMetricsDto> GetMetricsAsync()
                => _contractRepository.GetPublicMetricsAsync();

        // ===========================================================
        // MÉTODO PRINCIPAL DE CREACIÓN DE CONTRATO
        // ===========================================================
        public override async Task<ContractSelectDto> CreateAsync(ContractCreateDto dto)
        {
            BusinessValidationHelper.ThrowIfNull(dto, "El DTO no puede ser nulo.");
            ValidatePayload(dto);

            Contract? createdContract = null;
            int personId = 0;

            await _uow.ExecuteAsync(async ct =>
            {
                // ==========================================================
                // 1️⃣ Verificar existencia de persona
                // ==========================================================
                var existingPerson = await _personService.GetByDocumentAsync(dto.Document);

                if (existingPerson == null)
                {
                    // ======================================================
                    // 2️⃣ Registrar nueva persona y usuario mediante AuthService
                    // ======================================================
                    var registerDto = _mapper.Map<RegisterDto>(dto);
                    registerDto.Password = string.Empty; // Ignorado en RegisterInternalAsync

                    var registeredUser = await _authService.RegisterInternalAsync(registerDto);
                    if (registeredUser.PersonId <= 0)
                        throw new BusinessException("El registro de usuario no devolvi� un PersonId v�lido.");

                    personId = registeredUser.PersonId;
                }
                else
                {
                    // ======================================================
                    // 3️⃣ Si la persona ya existe, reutilizar su ID
                    // ======================================================
                    personId = existingPerson.Id;
                }

                // ==========================================================
                // 4️⃣ Reservar establecimientos y calcular renta base total
                // ==========================================================
                var (baseRent, uvtQty) = await _establishmentService.ReserveForContractAsync(dto.EstablishmentIds);

                // ==========================================================
                // 5️⃣ Crear contrato con cláusulas y locales
                // ==========================================================
                var contract = BuildContract(dto, personId, baseRent, uvtQty);
                createdContract = await _contractRepository.AddAsync(contract);

                // Forzar persistencia para obtener IDs
                await _uow.SaveChangesAsync(ct);
            });

            // ==========================================================
            // 6️⃣ Operaciones post-commit (correo, PDF, obligaciones)
            // ==========================================================
            if (!string.IsNullOrWhiteSpace(dto.Email) && createdContract != null)
            {
                try
                {
                    var person = await _personService.GetByIdAsync(personId);
                    var fullName = ComposeFullName(person);
                    var snapshot = await BuildSnapshotAsync(createdContract);

                    await SchedulePostCommitAsync(createdContract.Id, dto.Email, fullName, snapshot);

                    _logger.LogInformation(
                        "Correo y generación de obligaciones programadas correctamente para contrato {ContractId}.",
                        createdContract.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error en operaciones post-creación (correo/obligaciones) para contrato {ContractId}.",
                        createdContract?.Id);
                }
            }

            // ==========================================================
            // 7️⃣ Retornar DTO del contrato creado
            // ==========================================================
            return _mapper.Map<ContractSelectDto>(createdContract!);
        }

        // ===========================================================
        // MÉTODO DE BARRIDO DE EXPIRACIÓN
        // ===========================================================
        public async Task<ExpirationSweepResult> RunExpirationSweepAsync()
        {
            return await _uow.ExecuteAsync(async ct =>
            {
                var deactivated = (await _contractRepository.DeactivateExpiredAsync(DateTime.UtcNow)).ToList();
                var released = await _contractRepository.ReleaseEstablishmentsForExpiredAsync(DateTime.UtcNow);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Barrido de expiración: {DeactivatedCount} contratos desactivados, {ReleasedCount} establecimientos liberados.",
                    deactivated.Count, released
                );

                return new ExpirationSweepResult(deactivated, released);
            });
        }

        // ===========================================================
        // MÉTODOS AUXILIARES
        // ===========================================================
        private void ValidatePayload(ContractCreateDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            if (dto.EstablishmentIds is null || dto.EstablishmentIds.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");
        }

        private Contract BuildContract(ContractCreateDto dto, int personId, decimal totalBaseRent, decimal totalUvt)
        {
            var contract = _mapper.Map<Contract>(dto);
            contract.PersonId = personId;
            contract.TotalBaseRentAgreed = totalBaseRent;
            contract.TotalUvtQtyAgreed = totalUvt;
            contract.Active = true;

            // Asociar locales arrendados
            contract.PremisesLeased = dto.EstablishmentIds
                .Select(eid => new PremisesLeased { EstablishmentId = eid })
                .ToList();

            // Asociar cláusulas seleccionadas
            if (dto.ClauseIds is { Count: > 0 })
            {
                contract.ContractClauses = dto.ClauseIds
                    .Distinct()
                    .Select(cid => new ContractClause { ClauseId = cid })
                    .ToList();
            }

            return contract;
        }

        private async Task<ContractSelectDto?> BuildSnapshotAsync(Contract contract)
        {
            var loaded = await _contractRepository.GetByIdAsync(contract.Id);
            return _mapper.Map<ContractSelectDto>(loaded ?? contract);
        }

        private string ComposeFullName(PersonSelectDto person)
            => $"{person.FirstName} {person.LastName}".Trim();

        private async Task SchedulePostCommitAsync(
            int contractId,
            string? email,
            string fullName,
            ContractSelectDto? contractSnapshot)
        {
            // ---------- Generar obligaciones después del commit ----------
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

            // ---------- Enviar correo con PDF ----------
            if (!string.IsNullOrWhiteSpace(email) && contractSnapshot != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var pdf = await _contractPdfService.GeneratePdfAsync(contractSnapshot);
                        await _emailService.SendContractWithPdfAsync(email!, fullName, contractId.ToString("D6"), pdf);

                        _logger.LogInformation("Correo de contrato enviado a {Email} con PDF {ContractId}", email, contractId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error enviando correo/PDF del contrato {ContractId} a {Email}. El contrato sigue creado.",
                            contractId, email);
                    }
                });
            }
        }

        // ===========================================================
        // CONSULTAS Y FILTROS
        // ===========================================================
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
