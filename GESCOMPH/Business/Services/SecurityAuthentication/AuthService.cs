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
using Microsoft.Extensions.Options;
using Utilities.Exceptions;
using Utilities.Helpers.Business;
using Utilities.Helpers.GeneratePassword;
using Utilities.Messaging.Interfaces;
using System.Security.Cryptography;

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
        
        ITwoFactorCodeRepository twoFactorCodeRepository,
        IOptions<TwoFactorSettings> twoFactorOptions,
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
        private readonly ITwoFactorCodeRepository _twoFactorRepository = twoFactorCodeRepository;
        private readonly TwoFactorSettings _twoFactorSettings = twoFactorOptions.Value ?? new TwoFactorSettings();
        private readonly IPasswordResetCodeRepository _passwordResetRepo = passwordResetRepo;
        private readonly IUserContextService _userContext = userContextService;
        private readonly IPersonRepository _personRepository = personRepository;
        private readonly IToken _tokenService = tokenService;
        private readonly IUnitOfWork _uow = uow;


        public async Task<LoginResultDto> LoginAsync(LoginDto dto)
        {
            var user = await AuthenticateAsync(dto);

            var roles = await _rolUserData.GetRoleNamesByUserIdAsync(user.Id);
            var userDto = BuildUserAuthDto(user, roles);

            if (!user.TwoFactorEnabled)
                return LoginResultDto.FromTokens(await _tokenService.GenerateTokensAsync(userDto));

            var challenge = await CreateTwoFactorChallengeAsync(user);
            return LoginResultDto.FromChallenge(challenge);
        }


        public async Task<User> AuthenticateAsync(LoginDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            var normalizedEmail = NormalizeEmail(dto.Email);

            var user = await _userRepository.GetAuthUserByEmailAsync(normalizedEmail)
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

        public async Task<TokenResponseDto> ConfirmTwoFactorAsync(TwoFactorVerifyDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            var email = NormalizeEmail(dto.Email);
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email inválido.");

            var user = await _userRepository.GetByEmailAsync(email)
                ?? throw new ValidationException("Usuario no encontrado.");

            if (!user.TwoFactorEnabled)
                throw new ValidationException("La verificación de dos factores no está habilitada para este usuario.");

            var code = dto.Code?.Trim() ?? string.Empty;
            var record = await _twoFactorRepository.GetValidCodeAsync(user.Id, code)
                ?? throw new ValidationException("Código inválido o expirado.");

            record.IsUsed = true;
            record.UsedAt = DateTime.UtcNow;
            await _twoFactorRepository.UpdateAsync(record);

            var roles = await _rolUserData.GetRoleNamesByUserIdAsync(user.Id);
            var userDto = BuildUserAuthDto(user, roles);

            return await _tokenService.GenerateTokensAsync(userDto);
        }

        public async Task<TwoFactorChallengeDto> ResendTwoFactorCodeAsync(string email)
        {
            var normalized = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException("Email inválido.");

            var user = await _userRepository.GetByEmailAsync(normalized)
                ?? throw new ValidationException("Usuario no encontrado.");

            if (!user.TwoFactorEnabled)
                throw new ValidationException("La verificación de dos factores no está habilitada para este usuario.");

            return await CreateTwoFactorChallengeAsync(user);
        }

        public async Task ToggleTwoFactorAsync(int userId, bool enabled)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new ValidationException("Usuario no encontrado.");

            if (user.TwoFactorEnabled == enabled)
            {
                return;
            }

            user.TwoFactorEnabled = enabled;
            await _userRepository.UpdateAsync(user);

            if (!enabled)
            {
                await _twoFactorRepository.InvalidatePendingCodesAsync(user.Id);
            }

            _userContext.InvalidateCache(user.Id);
        }

        // El método original ahora usa el método interno dentro de su propia transacción
        public async Task<RegisterResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
        {
            return await _uow.ExecuteAsync(async token =>
            {
                var result = await RegisterInternalAsync(dto, token);
                await _uow.SaveChangesAsync(token);
                return result;
            }, ct);
        }


        public async Task<RegisterResultDto> RegisterInternalAsync(RegisterDto dto, CancellationToken ct = default)
        {
            // Validaciones
            if (dto == null) throw new BusinessException("Payload inválido.");
            ct.ThrowIfCancellationRequested();

            dto.Email = dto.Email.Trim().ToLowerInvariant();
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                throw new BusinessException("El correo ya está registrado.");

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

            var roleIds = (dto.RoleIds ?? Array.Empty<int>()).Distinct().ToList();
            if (roleIds.Any())
                await _rolUserData.ReplaceUserRolesAsync(user.Id, roleIds);
            else
                await _rolUserData.AsignateRolDefaultAsync(user);

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

            return new RegisterResultDto
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

        private UserAuthDto BuildUserAuthDto(User user, IEnumerable<string> roles)
        {
            var userDto = _mapper.Map<UserAuthDto>(user);
            userDto.Roles = roles;
            return userDto;
        }

        private async Task<TwoFactorChallengeDto> CreateTwoFactorChallengeAsync(User user)
        {
            var now = DateTime.UtcNow;
            var existing = await _twoFactorRepository.GetLatestActiveCodeAsync(user.Id);

            if (existing != null)
            {
                var elapsedSeconds = (now - existing.CreatedAt).TotalSeconds;
                if (elapsedSeconds <= _twoFactorSettings.ResendCooldownSeconds)
                    return BuildChallenge(user, existing);
            }

            if (existing != null)
                await _twoFactorRepository.InvalidatePendingCodesAsync(user.Id);

            var newCode = new TwoFactorCode
            {
                UserId = user.Id,
                Code = GenerateVerificationCode(),
                ExpiresAt = now.AddMinutes(_twoFactorSettings.ExpirationMinutes)
            };

            await _twoFactorRepository.AddAsync(newCode);

            try
            {
                await _emailService.SendTwoFactorCodeEmailAsync(
                    user.Email,
                    newCode.Code,
                    _twoFactorSettings.ExpirationMinutes,
                    _twoFactorSettings.EmailSubject);
            }
            catch (Exception ex)
            {
                await _twoFactorRepository.InvalidatePendingCodesAsync(user.Id);
                _logger.LogWarning(ex, "Error al enviar el código de doble factor a {Email}", user.Email);
                throw new BusinessException("No se pudo enviar el código de verificación. Intenta nuevamente.");
            }

            return BuildChallenge(user, newCode);
        }

        private static TwoFactorChallengeDto BuildChallenge(User user, TwoFactorCode code)
        {
            var expiresInSeconds = Math.Max(0, (int)(code.ExpiresAt - DateTime.UtcNow).TotalSeconds);
            return new TwoFactorChallengeDto
            {
                Email = user.Email,
                ExpiresAt = code.ExpiresAt,
                ExpiresInSeconds = expiresInSeconds
            };
        }

        private string GenerateVerificationCode()
        {
            var length = Math.Max(4, _twoFactorSettings.CodeLength);
            var buffer = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer);

            var digits = new char[length];
            for (var i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + (buffer[i] % 10));
            }

            return new string(digits);
        }

        private static string NormalizeEmail(string? email)
            => string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

        //  Sedelega UserContextService:
        public Task<UserMeDto> BuildUserContextAsync(int userId)
            => _userContext.BuildUserContextAsync(userId);

    }
}


