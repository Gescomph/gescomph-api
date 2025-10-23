using Business.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication;
using Data.Interfaz.IDataImplement.Persons;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Entity.Domain.Models.Implements.Persons;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Entity.DTOs.Implements.SecurityAuthentication.Auth.RestPasword;
using Entity.DTOs.Implements.SecurityAuthentication.Me;
using Entity.DTOs.Implements.SecurityAuthentication.User;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Utilities.Exceptions;
using Utilities.Helpers.GeneratePassword;
using Utilities.Messaging.Interfaces;
using System.Linq;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio encargado de la autenticación, manejo de contraseñas y construcción del contexto de usuario (/me).
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IRolUserService _rolUserData;
        private readonly ILogger<AuthService> _logger;
        private readonly IMapper _mapper;
        private readonly ISendCode _emailService;
        private readonly IPasswordResetCodeRepository _passwordResetRepo;
        private readonly IUserContextService _userContext;
        private readonly IPersonRepository _personRepository;
        private readonly IToken _tokenService;
        private readonly IRolUserRepository _rolUserRepository;
        private readonly IUnitOfWork _uow;

        public AuthService(
            IPasswordHasher<User> passwordHasher,
            IUserRepository userRepository,
            ILogger<AuthService> logger,
            IRolUserService rolUserData,
            IRolUserRepository rolUserRepository,
            IMapper mapper,
            ISendCode emailService,
            IPasswordResetCodeRepository passwordResetRepo,
            IUserContextService userContextService,
            IPersonRepository personRepository,
            IToken tokenService,
            IUnitOfWork uow
        )
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _logger = logger;
            _rolUserData = rolUserData;
            _rolUserRepository = rolUserRepository;
            _mapper = mapper;
            _emailService = emailService;
            _passwordResetRepo = passwordResetRepo;
            _userContext = userContextService;
            _personRepository = personRepository;
            _tokenService = tokenService;
            _uow = uow;
        }

        /// <summary>
        /// Autentica un usuario con email y contraseña, y genera los tokens correspondientes.
        /// </summary>
        public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await AuthenticateAsync(dto);
            var roles = await _rolUserData.GetRoleNamesByUserIdAsync(user.Id);

            var userDto = _mapper.Map<UserAuthDto>(user);
            userDto.Roles = roles;

            return await _tokenService.GenerateTokensAsync(userDto);
        }

        /// <summary>
        /// Registra una nueva persona con su usuario asociado y asigna los roles proporcionados o el rol por defecto.
        /// </summary>
        public async Task<UserSelectDto> RegisterAsync(RegisterDto dto)
        {
            var normalizedEmail = (dto.Email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                throw new BusinessException("El correo es obligatorio.");

            if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
                throw new BusinessException("El correo ya se encuentra registrado.");

            var document = dto.Document?.Trim();
            if (!string.IsNullOrWhiteSpace(document) && await _personRepository.ExistsByDocumentAsync(document))
                throw new BusinessException("Ya existe una persona con este número de documento.");

            var password = (dto.Password ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(password))
            {
                password = PasswordGenerator.Generate(12);
            }

            var roleIds = (dto.RoleIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToList();
            User? createdUser = null;

            await _uow.ExecuteAsync(async ct =>
            {
                var person = _mapper.Map<Person>(dto);
                person.Document = document;
                await _personRepository.AddAsync(person);

                var user = new User
                {
                    Email = normalizedEmail,
                    PersonId = person.Id
                };

                user.Password = _passwordHasher.HashPassword(user, password);

                await _userRepository.AddAsync(user);

                if (roleIds.Count > 0)
                {
                    await _rolUserRepository.ReplaceUserRolesAsync(user.Id, roleIds);
                }
                else
                {
                    await _rolUserRepository.AsignateRolDefault(user);
                }

                if (dto.SendPasswordByEmail && !string.IsNullOrWhiteSpace(password))
                {
                    var emailToSend = normalizedEmail;
                    var fullName = $"{dto.FirstName} {dto.LastName}".Trim();
                    var passwordCopy = password;

                    _uow.RegisterPostCommit(async _ =>
                    {
                        try
                        {
                            await _emailService.SendTemporaryPasswordAsync(emailToSend, fullName, passwordCopy);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudo enviar el correo de bienvenida a {Email}.", emailToSend);
                        }
                    });
                }

                createdUser = user;
            });

            if (createdUser is null)
                throw new BusinessException("No se pudo completar el registro de usuario.");

            var registered = await _userRepository.GetByIdWithDetailsAsync(createdUser.Id)
                             ?? throw new BusinessException("No se pudo obtener la información del usuario registrado.");

            var result = _mapper.Map<UserSelectDto>(registered);
            result.Roles = registered.RolUsers
                .Where(ru => ru.Rol is not null && !string.IsNullOrWhiteSpace(ru.Rol.Name))
                .Select(ru => ru.Rol!.Name)
                .ToList();
            return result;
        }

        /// <summary>
        /// Valida las credenciales del usuario y devuelve la entidad correspondiente si son correctas.
        /// </summary>
        public async Task<User> AuthenticateAsync(LoginDto dto)
        {
            var user = await _userRepository.GetAuthUserByEmailAsync(dto.Email)
                ?? throw new UnauthorizedAccessException("Usuario o contraseña inválida.");

            var pwdResult = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.Password);
            if (pwdResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Usuario o contraseña inválida.");

            if (user.IsDeleted)
                throw new UnauthorizedAccessException("La cuenta está eliminada o bloqueada.");

            if (!user.Active)
                throw new UnauthorizedAccessException("La cuenta está inactiva. Contacta al administrador.");

            return user;
        }

        /// <summary>
        /// Solicita un código temporal de recuperación de contraseña para el usuario asociado al email.
        /// </summary>
        public async Task RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email)
                ?? throw new ValidationException("Correo no registrado");

            var code = new Random().Next(100000, 999999).ToString();

            var resetCode = new PasswordResetCode
            {
                Email = email,
                Code = code,
                Expiration = DateTime.UtcNow.AddMinutes(10)
            };

            await _passwordResetRepo.AddAsync(resetCode);
            await SendRecoveryCodeEmailAsync(email, code);
        }

        /// <summary>
        /// Confirma un código de recuperación válido y establece una nueva contraseña para el usuario.
        /// </summary>
        public async Task ResetPasswordAsync(ConfirmResetDto dto)
        {
            var record = await _passwordResetRepo.GetValidCodeAsync(dto.Email, dto.Code)
                ?? throw new ValidationException("Código inválido o expirado");

            var user = await _userRepository.GetByEmailAsync(dto.Email)
                ?? throw new ValidationException("Usuario no encontrado");

            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, dto.NewPassword);

            await _userRepository.UpdateAsync(user);

            record.IsUsed = true;
            await _passwordResetRepo.UpdateAsync(record);

            _userContext.InvalidateCache(user.Id);
        }

        /// <summary>
        /// Permite a un usuario autenticado cambiar su contraseña actual.
        /// </summary>
        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId)
                       ?? throw new BusinessException("Usuario no encontrado.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
                throw new BusinessException("La contraseña actual es incorrecta.");

            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
            await _userRepository.UpdateAsync(user);
        }

        /// <summary>
        /// Construye el contexto (/me) del usuario autenticado, incluyendo roles y permisos.
        /// </summary>
        public Task<UserMeDto> BuildUserContextAsync(int userId)
            => _userContext.BuildUserContextAsync(userId);

        /// <summary>
        /// Envía un código de recuperación de contraseña al correo del usuario.
        /// </summary>
        private async Task SendRecoveryCodeEmailAsync(string email, string code)
        {
            try
            {
                await _emailService.SendRecoveryCodeEmail(email, code);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el código de recuperación al correo: {Email}", email);
            }
        }
    }
}
