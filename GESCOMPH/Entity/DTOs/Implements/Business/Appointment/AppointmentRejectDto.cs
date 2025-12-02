using Entity.DTOs.Base;

namespace Entity.DTOs.Implements.Business.Appointment
{
    public class AppointmentRejectDto : BaseDto
    {
        public string? Observation { get; set; }
    }
}
