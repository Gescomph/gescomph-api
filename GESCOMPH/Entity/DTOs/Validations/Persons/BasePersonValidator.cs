using System;
using System.Reflection;
using FluentValidation;

namespace Entity.DTOs.Validations.Persons
{
    public abstract class BasePersonValidator<T> : AbstractValidator<T>
    {
        protected BasePersonValidator()
        {
            var type = typeof(T);

            if (HasProperty(type, "FirstName"))
            {
                RuleFor(x => (string?)Get(x, "FirstName"))
                  .NotEmpty().WithMessage("El nombre es obligatorio.")
                  .MaximumLength(50)
                  .Matches(@"^[\p{L}\p{M}\s'-]+$").WithMessage("El nombre contiene caracteres invalidos.");
            }

            if (HasProperty(type, "LastName"))
            {
                RuleFor(x => (string?)Get(x, "LastName"))
                  .NotEmpty().WithMessage("El apellido es obligatorio.")
                  .MaximumLength(50)
                  .Matches(@"^[\p{L}\p{M}\s'-]+$").WithMessage("El apellido contiene caracteres invalidos.");
            }

            if (HasProperty(type, "Phone"))
            {
                RuleFor(x => (string?)Get(x, "Phone"))
                  .NotEmpty().WithMessage("El numero de telefono es obligatorio.")
                  .Matches(@"^\+?\d{7,15}$").WithMessage("El numero de telefono debe tener entre 7 y 15 digitos.");
            }

            if (HasProperty(type, "Address"))
            {
                RuleFor(x => (string?)Get(x, "Address"))
                  .NotEmpty().WithMessage("La direccion es obligatoria.")
                  .MaximumLength(100)
                  .Matches(@"^[\w\s\.\-#]+$").WithMessage("La direccion contiene caracteres invalidos.");
            }

            if (HasProperty(type, "CityId"))
            {
                // CityId como nullable para no castear null a int
                RuleFor(x => (int?)Get(x, "CityId"))
                  .NotNull().WithMessage("Debe seleccionar una ciudad valida.")
                  .GreaterThan(0).WithMessage("Debe seleccionar una ciudad valida.");
            }
        }

        private static bool HasProperty(Type type, string propertyName)
            => type.GetProperty(
                  propertyName,
                  BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase
               ) is not null;

        // Hacemos el helper accesible y tolerante: ignora may/min y no lanza
        protected static object? Get(object? instance, string propertyName)
        {
            if (instance is null) return null;
            var prop = instance.GetType().GetProperty(
              propertyName,
              BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase
            );
            return prop?.GetValue(instance);
        }
    }
}
