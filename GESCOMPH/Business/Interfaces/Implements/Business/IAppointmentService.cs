using Business.Interfaces.IBusiness;
using Entity.DTOs.Implements.Business.Appointment;
using Entity.DTOs.Implements.Business.Contract;
using Entity.Enum;

namespace Business.Interfaces.Implements.Business
{
    public interface IAppointmentService : IBusiness<AppointmentSelectDto, AppointmentCreateDto, AppointmentUpdateDto>
    {
        Task<IEnumerable<AppointmentSelectDto>> GetAppointmentByDate(DateOnly date);
        Task<IEnumerable<AppointmentSelectDto>> GetAllByPersonId(int personId);

        // Métodos para gestionar el estado de la cita
        Task<AppointmentSelectDto> AcceptAppointmentAsync(int appointmentId);
        Task<AppointmentSelectDto> RejectAppointmentAsync(int appointmentId, string? observation);
        Task<AppointmentSelectDto> UpdateStatusAsync(int appointmentId, Status status, string? observation);
    }
}
