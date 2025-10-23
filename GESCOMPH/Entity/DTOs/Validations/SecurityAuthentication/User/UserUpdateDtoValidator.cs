using Entity.DTOs.Implements.SecurityAuthentication.User;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace Entity.DTOs.Validations.SecurityAuthentication.User
{
    public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
    {
        public UserUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                    .WithMessage("El identificador es obligatorio.");

            RuleFor(x => x.PersonId)
                .GreaterThan(0)
                    .WithMessage("El identificador de la persona es obligatorio.");

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage("El correo es obligatorio.")
                .Must(IsValidEmail)
                    .WithMessage("El correo no tiene un formato valido.");

            RuleFor(x => x.Password)
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
                .MaximumLength(100).WithMessage("La contraseña no puede exceder los 100 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Password));

            RuleFor(x => x.RoleIds)
                .Must(AllPositive)
                    .WithMessage("Todos los roles deben tener identificadores positivos.")
                .Must(AllDistinct)
                    .WithMessage("No se permiten roles duplicados.");
        }

        private static bool AllPositive(IReadOnlyCollection<int>? ids)
            => ids is null || ids.All(id => id > 0);

        private static bool AllDistinct(IReadOnlyCollection<int>? ids)
            => ids is null || ids.Distinct().Count() == ids.Count;

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                _ = new MailAddress(email.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
