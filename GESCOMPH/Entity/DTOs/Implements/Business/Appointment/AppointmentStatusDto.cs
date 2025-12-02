  using Entity.DTOs.Base;
using Entity.Enum;

namespace Entity.DTOs.Implements.Business.Appointment
{
    public class AppointmentStatusDto : BaseDto
    {
        public Status Status { get; set; }
        public string? Observation { get; set; }
    }
}
