using Business.Interfaces;
using Business.Interfaces.Implements.Business;
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

        public AppointmentService(
            IAppointmentRepository data,
            IMapper mapper,
            IPersonService personService,
            IUserService userService,
            ISendCode emailService,
            IAuthService authService,
            IUnitOfWork uow,
            ILogger<AppointmentService> logger
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
                    registerDto.Password = string.Empty;

                    RegisterDto? registeredUser;
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

                    if (registeredUser?.PersonId is null or 0)
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
    }
}
