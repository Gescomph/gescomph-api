using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.IDataImplement.Persons;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Domain.Models.Implements.Persons;
using Entity.DTOs.Implements.SecurityAuthentication.User;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Helpers.GeneratePassword;
using Utilities.Messaging.Interfaces;
using Business.Interfaces;

namespace Business.Services.SecurityAuthentication
{
    public class UserService
        : BusinessGeneric<UserSelectDto, UserCreateDto, UserUpdateDto, User>, IUserService
    {
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IRolUserRepository _rolUserRepository;
        private readonly IPersonRepository _personRepository;
        private readonly ISendCode _emailService;
        private readonly IUnitOfWork _uow;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher,
            IRolUserRepository rolUserRepository,
            IPersonRepository personRepository,
            ISendCode emailService,
            IUnitOfWork uow
        ) : base(userRepository, mapper)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _rolUserRepository = rolUserRepository;
            _personRepository = personRepository;
            _emailService = emailService;
            _uow = uow;
        }

        // ======================================================
        // =================== MÉTODOS DE LECTURA ===============
        // ======================================================

        /// <summary>
        /// Obtiene todos los usuarios con sus roles asociados.
        /// </summary>
        public override async Task<IEnumerable<UserSelectDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var result = new List<UserSelectDto>(capacity: users.Count());

            foreach (var u in users)
            {
                var dto = _mapper.Map<UserSelectDto>(u);
                dto.Roles = await _rolUserRepository.GetRoleNamesByUserIdAsync(u.Id);
                result.Add(dto);
            }

            return result;
        }

        // ======================================================
        // ================== CREACIÓN COMPLETA =================
        // ======================================================

        /// <summary>
        /// Crea un nuevo usuario junto con su persona y roles asociados.
        /// Genera una contraseña temporal y la envía por correo.
        /// </summary>
        /// <param name="dto">Datos de creación del usuario.</param>
        public override async Task<UserSelectDto> CreateAsync(UserCreateDto dto)
        {
            var normalizedEmail = await ValidateUserEmailAndPersonAsync(
                dto.Email,
                dto.PersonId);

            var person = await _personRepository.GetByIdAsync(dto.PersonId)
                         ?? throw new BusinessException("La persona asociada no fue encontrada.");

            var plainPassword = ResolvePassword(dto.Password);
            var user = CreateUserEntity(dto, normalizedEmail, plainPassword);
            var shouldSendPassword = dto.SendPasswordByEmail && !string.IsNullOrWhiteSpace(plainPassword);
            var fullName = ComposePersonFullName(person);

            await _uow.ExecuteAsync(async ct =>
            {
                await _userRepository.AddAsync(user);
                await AssignRolesAsync(user, dto.RoleIds);
                if (shouldSendPassword)
                {
                    QueuePasswordEmail(normalizedEmail, fullName, plainPassword);
                }
            });

            var created = await _userRepository.GetByIdWithDetailsAsync(user.Id)
                          ?? throw new Exception("No se pudo recuperar el usuario tras registrarlo.");

            var result = _mapper.Map<UserSelectDto>(created);
            result.Roles = (await _rolUserRepository.GetRoleNamesByUserIdAsync(created.Id)).ToList();
            return result;
        }

        // ======================================================
        // =================== ACTUALIZACIÓN ====================
        // ======================================================

        /// <summary>
        /// Actualiza un usuario junto con sus datos personales y roles asociados.
        /// </summary>
        /// <param name="dto">Datos de actualización del usuario.</param>
        public override async Task<UserSelectDto> UpdateAsync(UserUpdateDto dto)
        {
            var user = await _userRepository.GetByIdForUpdateAsync(dto.Id)
                       ?? throw new BusinessException("Usuario no encontrado.");

            var normalizedEmail = await ValidateUserEmailAndPersonAsync(
                dto.Email,
                dto.PersonId,
                dto.Id,
                dto.PersonId != user.PersonId);

            var person = await _personRepository.GetByIdAsync(dto.PersonId)
                         ?? throw new BusinessException("La persona asociada no fue encontrada.");

            var emailChanged = !string.Equals(
                user.Email,
                normalizedEmail,
                StringComparison.OrdinalIgnoreCase);

            await _uow.ExecuteAsync(async ct =>
            {
                user.Email = normalizedEmail;
                user.PersonId = dto.PersonId;

                if (dto.Active.HasValue)
                {
                    user.Active = dto.Active.Value;
                }

                await _userRepository.UpdateAsync(user);

                if (dto.RoleIds is not null)
                {
                    var roleIds = dto.RoleIds
                        .Where(x => x > 0)
                        .Distinct()
                        .ToList();

                    if (roleIds.Count > 0)
                    {
                        await _rolUserRepository.ReplaceUserRolesAsync(user.Id, roleIds);
                    }
                    else
                    {
                        await _rolUserRepository.AsignateRolDefault(user);
                    }
                }

                if (dto.SendPasswordByEmail
                    && emailChanged
                    && !string.IsNullOrWhiteSpace(dto.Password))
                {
                    var email = normalizedEmail;
                    var fullName = $"{person.FirstName ?? string.Empty} {person.LastName ?? string.Empty}".Trim();
                    var passwordCopy = dto.Password!.Trim();

                    _uow.RegisterPostCommit(async _ =>
                    {
                        await SendTemporaryPasswordAsync(email, fullName, passwordCopy!);
                    });
                }
            });

            var updated = await _userRepository.GetByIdWithDetailsAsync(user.Id)
                          ?? throw new Exception("No se pudo recuperar el usuario actualizado.");

            var result = _mapper.Map<UserSelectDto>(updated);
            result.Roles = (await _rolUserRepository.GetRoleNamesByUserIdAsync(updated.Id)).ToList();
            return result;
        }

        protected override Expression<Func<User, string?>>[] SearchableFields() =>
        [
            x => x.Email
        ];

        protected override string[] SortableFields() => new[]
        {
            nameof(User.Email),
            nameof(User.PersonId),
            nameof(User.Active),
            nameof(User.CreatedAt),
            nameof(User.Id)
        };

        protected override IDictionary<string, LambdaExpression> SortMap()
            => new Dictionary<string, LambdaExpression>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(User.Email)] = (Expression<Func<User, string>>)(u => u.Email),
                [nameof(User.PersonId)] = (Expression<Func<User, int>>)(u => u.PersonId),
                [nameof(User.Active)] = (Expression<Func<User, bool>>)(u => u.Active),
                [nameof(User.CreatedAt)] = (Expression<Func<User, DateTime>>)(u => u.CreatedAt),
                [nameof(User.Id)] = (Expression<Func<User, int>>)(u => u.Id),
            };

        protected override IDictionary<string, Func<string, Expression<Func<User, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<User, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(User.Email)] = value => x => x.Email == value,
                [nameof(User.PersonId)] = value => x => x.PersonId == int.Parse(value),
                [nameof(User.Active)] = value => x => x.Active == bool.Parse(value),
                [nameof(User.Id)] = value => x => x.Id == int.Parse(value)
            };

        private async Task<string> ValidateUserEmailAndPersonAsync(
            string? email,
            int personId,
            int? currentUserId = null,
            bool validatePersonAssociation = true)
        {
            var normalizedEmail = (email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedEmail))
                throw new BusinessException("El correo es obligatorio.");

            var emailExists = currentUserId.HasValue
                ? await _userRepository.ExistsByEmailExcludingIdAsync(currentUserId.Value, normalizedEmail)
                : await _userRepository.ExistsByEmailAsync(normalizedEmail);

            if (emailExists)
            {
                var message = currentUserId.HasValue
                    ? "El correo ya está registrado por otro usuario."
                    : "El correo ya está registrado.";
                throw new BusinessException(message);
            }

            if (validatePersonAssociation)
            {
                var existingByPerson = await _userRepository.GetByPersonIdAsync(personId);
                if (existingByPerson is not null && existingByPerson.Id != currentUserId)
                    throw new BusinessException("La persona seleccionada ya tiene un usuario asociado.");
            }

            return normalizedEmail;
        }

        private User CreateUserEntity(UserCreateDto dto, string normalizedEmail, string plainPassword)
        {
            var user = _mapper.Map<User>(dto);
            user.Email = normalizedEmail;
            user.Password = _passwordHasher.HashPassword(user, plainPassword);
            return user;
        }

        private async Task AssignRolesAsync(User user, IReadOnlyCollection<int>? roleIds)
        {
            var normalizedRoleIds = (roleIds ?? Array.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (normalizedRoleIds.Count > 0)
            {
                await _rolUserRepository.ReplaceUserRolesAsync(user.Id, normalizedRoleIds);
            }
            else
            {
                await _rolUserRepository.AsignateRolDefault(user);
            }
        }

        public void QueuePasswordEmail(string email, string fullName, string password)
        {
            _uow.RegisterPostCommit(async _ =>
            {
                await SendTemporaryPasswordAsync(email, fullName, password);
            });
        }

        private static string ComposePersonFullName(Person person)
            => $"{person.FirstName ?? string.Empty} {person.LastName ?? string.Empty}".Trim();

        private static string ResolvePassword(string? incomingPassword)
        {
            var trimmed = incomingPassword?.Trim();
            return string.IsNullOrWhiteSpace(trimmed)
                ? PasswordGenerator.Generate(12)
                : trimmed;
        }

        // ======================================================
        // =========== CREACIÓN CON PERSONA EXISTENTE ===========
        // ======================================================

        /// <summary>
        /// Garantiza que exista un usuario asociado a una persona.
        /// Si no existe, lo crea con una contraseña temporal.
        /// </summary>
        /// <param name="personId">Identificador de la persona.</param>
        /// <param name="email">Correo electrónico del usuario.</param>
        public async Task<(int userId, bool created, string? tempPassword)> EnsureUserForPersonAsync(int personId, string email)
        {
            if (personId <= 0)
                throw new BusinessException("PersonId inválido.");

            var normalizedEmail = (email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                throw new BusinessException("El correo es requerido.");

            var existing = await _userRepository.GetByPersonIdAsync(personId);
            if (existing is not null)
                return (existing.Id, false, null);

            if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
                throw new BusinessException("El correo ya está registrado.");

            if (await _personRepository.GetByIdAsync(personId) is null)
                throw new BusinessException("Persona no encontrada para crear el usuario.");

            int createdUserId = 0;
            var tempPassword = PasswordGenerator.Generate(12);

            await _uow.ExecuteAsync(async ct =>
            {
                var user = new User
                {
                    Email = normalizedEmail,
                    PersonId = personId
                };

                user.Password = _passwordHasher.HashPassword(user, tempPassword);

                await _userRepository.AddAsync(user);
                await _rolUserRepository.AsignateRolDefault(user);

                createdUserId = user.Id;
            });

            return (createdUserId, true, tempPassword);
        }

        // ======================================================
        // =============== MÉTODOS DE SOPORTE ===================
        // ======================================================

        /// <summary>
        /// Envía la contraseña temporal por correo electrónico.
        /// </summary>
        private async Task SendTemporaryPasswordAsync(string email, string fullName, string tempPassword)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(tempPassword))
                    await _emailService.SendTemporaryPasswordAsync(email, fullName, tempPassword);
            }
            catch
            {
                // No se interrumpe el flujo principal si el envío de correo falla.
            }
        }

    }
}
