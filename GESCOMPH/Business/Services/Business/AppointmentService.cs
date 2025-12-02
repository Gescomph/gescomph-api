using Business.Interfaces;
using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.AdministrationSystem;
using Business.Interfaces.Implements.Persons;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Business.Services.SecurityAuthentication;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Appointment;
using Entity.DTOs.Implements.Persons.Person;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Entity.Infrastructure.Context;
using Entity.DTOs.Implements.Utilities;
using Entity.Enum;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Helpers.Business;
using Utilities.Messaging.Interfaces;

namespace Business.Services.Business
{
    public class AppointmentService
        : BusinessGeneric<AppointmentSelectDto, AppointmentCreateDto, AppointmentUpdateDto, Appointment>,
          IAppointmentService
    {
        private readonly IAppointmentRepository _data;
        private readonly IMapper _mapper;
        private readonly IPersonService _personService;
        private readonly IUserService _userService;
        private readonly ISendCode _emailService;
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;

        public AppointmentService(
            IAppointmentRepository data,
            IMapper mapper,
            IPersonService personService,
            IUserService userService,
            ISendCode emailService,
            IAuthService authService,
            IUnitOfWork uow,
            ILogger<AppointmentService> logger,
            INotificationService notificationService
        ) : base(data, mapper)
        {
            _data = data;
            _mapper = mapper;
            _personService = personService;
            _userService = userService;
            _emailService = emailService;
            _authService = authService;
            _uow = uow;
            _logger = logger;
            _notificationService = notificationService;
        }

        public override async Task<AppointmentSelectDto> CreateAsync(AppointmentCreateDto dto)
        {
            BusinessValidationHelper.ThrowIfNull(dto, "El DTO no puede ser nulo.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new BusinessException("El correo electrónico es obligatorio para registrar la persona.");

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                throw new BusinessException("Nombre y apellido son obligatorios para registrar la persona.");

            if (string.IsNullOrWhiteSpace(dto.Document))
                throw new BusinessException("El documento es obligatorio para registrar la persona.");

            return await _uow.ExecuteAsync(async ct =>
            {
                Appointment? createdAppointment = null;
                int personId = 0;

                var existingPerson = await _personService.GetByDocumentAsync(dto.Document);

                if (existingPerson == null)
                {
                    var registerDto = _mapper.Map<RegisterDto>(dto);

                    RegisterResultDto? registeredUser;
                    try
                    {
                        // Usar el método interno que no crea su propia transacción
                        registeredUser = await _authService.RegisterInternalAsync(registerDto, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error registrando usuario desde cita. Email={Email}", dto.Email);
                        throw;
                    }

                    if (registeredUser == null || registeredUser.PersonId <= 0)
                        throw new BusinessException("El registro de usuario no devolvió un PersonId válido.");

                    personId = registeredUser.PersonId.Value;
                }
                else
                {
                    personId = existingPerson.Id;
                }

                var appointment = _mapper.Map<Appointment>(dto);
                appointment.PersonId = personId;
                appointment.Active = true;

                createdAppointment = await _data.AddAsync(appointment);
                await _uow.SaveChangesAsync(ct);

                // Registrar post-commit (por ejemplo, envío de correo o notificación)
                //_uow.RegisterPostCommit(async _ =>
                //{
                //    await _emailService.SendAppointmentConfirmationAsync(dto.Email, appointment.Id);
                //});

                return _mapper.Map<AppointmentSelectDto>(createdAppointment!);
            });
        }

        public async Task<IEnumerable<AppointmentSelectDto>> GetAppointmentByDate(DateOnly date)
        {
            try
            {
                if (date == default)
                {
                    _logger.LogWarning("Se intentó buscar citas con una fecha inválida");
                    throw new ArgumentException("La fecha proporcionada no es válida", nameof(date));
                }

                int year = date.Year;
                int month = date.Month;
                int day = date.Day;

                var appointments = await _data.GetAppointmentByDate(year, month, day);

                if (appointments == null || !appointments.Any())
                {
                    _logger.LogInformation("No se encontraron citas para la fecha: {date:yyyy-MM-dd}", date);
                    return Enumerable.Empty<AppointmentSelectDto>();
                }

                return _mapper.Map<IEnumerable<AppointmentSelectDto>>(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al traer las citas con la fecha {date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<AppointmentSelectDto>> GetAllByPersonId(int personId)
        {
            try
            {
                if (personId <= 0)
                {
                    _logger.LogWarning("Se intentó buscar citas con un personId inválido: {personId}", personId);
                    throw new ArgumentException("El ID de la persona debe ser mayor a 0", nameof(personId));
                }

                var appointments = await _data.GetAllByPersonId(personId);

                if (appointments == null || !appointments.Any())
                {
                    _logger.LogInformation("No se encontraron citas para la persona con ID: {personId}", personId);
                    return Enumerable.Empty<AppointmentSelectDto>();
                }

                return _mapper.Map<IEnumerable<AppointmentSelectDto>>(appointments);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las citas para la persona con ID: {personId}", personId);
                throw;
            }
        }

        protected override Expression<Func<Appointment, string>>[] SearchableFields() =>
        [
            a => a.Description!,
            a => a.Person.FirstName!,
            a => a.Person.LastName!,
            a => a.Person.Phone!,
            a => a.Establishment.Name!
        ];

        protected override string[] SortableFields() => new[]
        {
            nameof(Appointment.Description),
            nameof(Appointment.RequestDate),
            nameof(Appointment.DateTimeAssigned),
            nameof(Appointment.EstablishmentId),
            nameof(Appointment.PersonId),
            nameof(Appointment.Id),
            nameof(Appointment.CreatedAt),
            nameof(Appointment.Active)
        };

        protected override IDictionary<string, Func<string, Expression<Func<Appointment, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Appointment, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Appointment.EstablishmentId)] = v => e => e.EstablishmentId == int.Parse(v),
                [nameof(Appointment.PersonId)] = v => e => e.PersonId == int.Parse(v),
                [nameof(Appointment.Active)] = v => e => e.Active == bool.Parse(v),
                [nameof(Appointment.RequestDate)] = v => e => e.RequestDate == DateTime.Parse(v)
            };

        public async Task<AppointmentSelectDto> AcceptAppointmentAsync(int appointmentId)
        {
            return await UpdateStatusAsync(appointmentId, Entity.Enum.Status.Aprobada, null);
        }

        public async Task<AppointmentSelectDto> RejectAppointmentAsync(int appointmentId, string? observation)
        {
            return await UpdateStatusAsync(appointmentId, Entity.Enum.Status.Rechazada, observation);
        }

        public async Task<AppointmentSelectDto> UpdateStatusAsync(int appointmentId, Entity.Enum.Status status, string? observation)
        {
            try
            {
                if (appointmentId <= 0)
                {
                    _logger.LogWarning("Se intentó actualizar una cita con un ID inválido: {appointmentId}", appointmentId);
                    throw new ArgumentException("El ID de la cita debe ser mayor a 0", nameof(appointmentId));
                }

                var appointment = await _data.GetByIdAsync(appointmentId);

                if (appointment == null)
                {
                    _logger.LogWarning("No se encontró la cita con ID: {appointmentId}", appointmentId);
                    throw new BusinessException($"No se encontró la cita con ID {appointmentId}");
                }

                if (appointment.Status != Entity.Enum.Status.Pendiente)
                {
                    _logger.LogWarning("Se intentó actualizar una cita que no está en estado Pendiente. ID: {appointmentId}, Estado actual: {status}", appointmentId, appointment.Status);
                    throw new BusinessException($"Solo se pueden gestionar citas en estado Pendiente. Estado actual: {appointment.Status}");
                }

                if (status != Entity.Enum.Status.Aprobada && status != Entity.Enum.Status.Rechazada)
                {
                    _logger.LogWarning("Se intentó asignar un estado no permitido: {status}", status);
                    throw new BusinessException("Solo se permite actualizar a Aprobada o Rechazada.");
                }

                appointment.Status = status;

                if (status == Entity.Enum.Status.Rechazada && !string.IsNullOrWhiteSpace(observation))
                {
                    appointment.Observation = observation;
                }

                await _data.UpdateAsync(appointment);

                _logger.LogInformation("Estado de la cita actualizado. ID: {appointmentId}, Nuevo estado: {status}", appointmentId, status);

                // Notificar al usuario (si existe) después del commit
                var recipientUserId = appointment.Person?.User?.Id;
                var personFullName = $"{appointment.Person?.FirstName} {appointment.Person?.LastName}".Trim();
                var actionRoute = "/appointments";
                var dateLabel = appointment.DateTimeAssigned.HasValue
                    ? appointment.DateTimeAssigned.Value.ToString("yyyy-MM-dd HH:mm")
                    : "por programar";
                var message = status == Status.Aprobada
                    ? $"Tu cita #{appointment.Id} fue aprobada. Fecha/hora: {dateLabel}."
                    : $"Tu cita #{appointment.Id} fue rechazada. Motivo: {(string.IsNullOrWhiteSpace(observation) ? "Sin motivo especificado" : observation)}.";
                var title = status == Status.Aprobada ? "Cita aprobada" : "Cita rechazada";
                var priority = status == Status.Aprobada ? NotificationPriority.Info : NotificationPriority.Warning;

                if (recipientUserId.HasValue && recipientUserId.Value > 0)
                {
                    _uow.RegisterPostCommit(async _ =>
                    {
                        try
                        {
                            var notificationDto = new NotificationCreateDto
                            {
                                Title = title,
                                Message = message,
                                Type = NotificationType.System,
                                Priority = priority,
                                RecipientUserId = recipientUserId.Value,
                                ActionRoute = actionRoute
                            };

                            await _notificationService.CreateAsync(notificationDto);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creando notificaci�n de cita {AppointmentId}", appointmentId);
                        }
                    });
                }

                return _mapper.Map<AppointmentSelectDto>(appointment);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la cita con ID: {appointmentId}", appointmentId);
                throw;
            }
        }
    }
}
