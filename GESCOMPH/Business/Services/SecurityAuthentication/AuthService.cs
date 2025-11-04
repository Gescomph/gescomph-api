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
using Utilities.Helpers.Business;
using Utilities.Helpers.GeneratePassword;
using Utilities.Messaging.Interfaces;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio de autenticación. El contexto de usuario (/me) lo construye UserContextService.
    /// </summary>
    public class AuthService(
        IPasswordHasher<User> passwordHasher,
        IUserRepository userData,
        ILogger<AuthService> logger,
        IRolUserService rolUserData,
        IMapper mapper,
        ISendCode emailService,
        IPasswordResetCodeRepository passwordResetRepo,
        IUserContextService userContextService,
        IPersonRepository personRepository,
        IToken tokenService,
        IUnitOfWork uow
    ) : IAuthService
    {
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
        private readonly IUserRepository _userRepository = userData;
        private readonly IRolUserService _rolUserData = rolUserData;
        private readonly ILogger<AuthService> _logger = logger;
        private readonly IMapper _mapper = mapper;
        private readonly ISendCode _emailService = emailService;
        private readonly IPasswordResetCodeRepository _passwordResetRepo = passwordResetRepo;
        private readonly IUserContextService _userContext = userContextService;
        private readonly IPersonRepository _personRepository = personRepository;
        private readonly IToken _tokenService = tokenService;
        private readonly IUnitOfWork _uow = uow;


        public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await AuthenticateAsync(dto);

            var roles = await _rolUserData.GetRoleNamesByUserIdAsync(user.Id);

            var userDto = _mapper.Map<UserAuthDto>(user);
            userDto.Roles = roles;

            return await _tokenService.GenerateTokensAsync(userDto);
        }


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

        // El método original ahora usa el método interno dentro de su propia transacción
        public async Task<RegisterDto> RegisterAsync(RegisterDto dto)
        {
            return await RegisterInternalAsync(dto);
;
        }


        public async Task<RegisterDto> RegisterInternalAsync(RegisterDto dto, CancellationToken ct = default)
        {
            // Validaciones
            if (dto == null) throw new BusinessException("Payload inválido.");

            dto.Email = dto.Email.Trim().ToLowerInvariant();
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                throw new BusinessException("El correo ya está registrado.");

            // Crear persona y usuario (sin SaveChanges aún)
            var person = _mapper.Map<Person>(dto);
            await _personRepository.AddAsync(person);

            var tempPassword = PasswordGenerator.Generate(12);
            var user = new User
            {
                Email = dto.Email,
                Person = person,
                Password = _passwordHasher.HashPassword(null!, tempPassword)
            };
            await _userRepository.AddAsync(user);

            // Asignar roles
            var roleIds = (dto.RoleIds ?? Array.Empty<int>()).Distinct().ToList();
            if (roleIds.Any())
                await _rolUserData.ReplaceUserRolesAsync(user.Id, roleIds);
            else
                await _rolUserData.AsignateRolDefaultAsync(user);

            // Registrar post-commit (pero no ejecutar aún)
            _uow.RegisterPostCommit(async _ =>
            {
                try
                {
                    await _emailService.SendTemporaryPasswordAsync(dto.Email, $"{dto.FirstName} {dto.LastName}", tempPassword);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al enviar correo de bienvenida a {Email}", dto.Email);
                }
            });

            return new RegisterDto
            {
                Email = user.Email,
                PersonId = person.Id,
                RoleIds = roleIds
            };
        }


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
            await _emailService.SendRecoveryCodeEmail(email, code);
        }



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

            // Invalida contexto /me
            _userContext.InvalidateCache(user.Id);
        }


        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId)
                       ?? throw new BusinessException("Usuario no encontrado.");

            // Validar contraseña actual
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
                throw new BusinessException("La contraseña actual es incorrecta.");

            // Hashear nueva contraseña
            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);

            await _userRepository.UpdateAsync(user);
        }

        //  Sedelega UserContextService:
        public Task<UserMeDto> BuildUserContextAsync(int userId)
            => _userContext.BuildUserContextAsync(userId);

    }
}
