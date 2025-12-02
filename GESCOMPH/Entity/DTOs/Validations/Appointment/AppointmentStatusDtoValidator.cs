using Entity.DTOs.Implements.Business.Appointment;
using Entity.Enum;
using FluentValidation;

namespace Entity.DTOs.Validations.Appointment
{
    public class AppointmentStatusDtoValidator : AbstractValidator<AppointmentStatusDto>
    {
        public AppointmentStatusDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("El id de la cita es obligatorio.");

            RuleFor(x => x.Status)
                .Must(s => s == Status.Aprobada || s == Status.Rechazada)
                .WithMessage("Solo se permiten los estados Aprobada o Rechazada.");

            RuleFor(x => x.Observation)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Observation))
                .WithMessage("La observación no puede superar 500 caracteres.");
        }
    }
}
