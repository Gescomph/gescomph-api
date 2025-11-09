using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.Implements.Persons;
using Entity.DTOs.Implements.Business.Appointment;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Mapster;

namespace Business.Mapping.Registers
{
    /// <summary>
    /// Configuración de mapeos del módulo de negocio (Appointment).
    /// Define transformaciones entre entidades, DTOs del dominio y 
    /// DTOs de autenticación (registro de persona desde cita).
    /// </summary>
    public class BusinessAppointmentMapping : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // ==========================================================
            // Appointment ? AppointmentSelectDto
            // ==========================================================
            config.NewConfig<Appointment, AppointmentSelectDto>()
                .Map(dest => dest.EstablishmentName, src => src.Establishment.Name)
                .Map(dest => dest.PersonName, src => src.Person.FirstName + " " + src.Person.LastName)
                .Map(dest => dest.Phone, src => src.Person.Phone);

            // ==========================================================
            // Person ? AppointmentCreateDto
            // Permite precargar datos de persona al crear una cita
            // ==========================================================
            config.NewConfig<Person, AppointmentCreateDto>()
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Document, src => src.Document)
                .Map(dest => dest.Phone, src => src.Phone)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.CityId, src => src.CityId)
                // Campos propios de la cita, no de la persona
                .Ignore(dest => dest.Description)
                .Ignore(dest => dest.RequestDate)
                .Ignore(dest => dest.DateTimeAssigned)
                .Ignore(dest => dest.EstablishmentId)
                .Ignore(dest => dest.Active);

            // ==========================================================
            // Appointment ? AppointmentCreateDto
            // Evita re-mapeos recursivos de datos de persona
            // ==========================================================
            config.NewConfig<Appointment, AppointmentCreateDto>()
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.RequestDate, src => src.RequestDate)
                .Map(dest => dest.DateTimeAssigned, src => src.DateTimeAssigned)
                .Map(dest => dest.EstablishmentId, src => src.EstablishmentId)
                .Map(dest => dest.Active, src => src.Active)
                // Ignorar datos de persona
                .Ignore(dest => dest.FirstName)
                .Ignore(dest => dest.LastName)
                .Ignore(dest => dest.Document)
                .Ignore(dest => dest.Address)
                .Ignore(dest => dest.Phone)
                .Ignore(dest => dest.CityId)
                .Ignore(dest => dest.Email);

            // ==========================================================
            // AppointmentCreateDto ? RegisterDto
            // Mapeo transversal hacia contexto de autenticación
            // (la validación de Email y Document se realiza antes del mapeo)
            // ==========================================================
            config.NewConfig<AppointmentCreateDto, RegisterDto>()
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Document, src => src.Document ?? string.Empty)
                .Map(dest => dest.Phone, src => src.Phone ?? string.Empty)
                .Map(dest => dest.Address, src => src.Address ?? string.Empty)
                .Map(dest => dest.CityId, src => src.CityId)
                // Campos controlados por AuthService
                .Ignore(dest => dest.RoleIds)
                .Ignore(dest => dest.Password);
        }
    }
}
