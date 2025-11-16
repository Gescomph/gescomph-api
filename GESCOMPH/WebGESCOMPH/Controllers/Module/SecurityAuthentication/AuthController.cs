using Business.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Entity.DTOs.Implements.SecurityAuthentication.Auth.RestPasword;
using Entity.DTOs.Implements.SecurityAuthentication.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebGESCOMPH.Infrastructure;

namespace WebGESCOMPH.Controllers.Module.SecurityAuthentication
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;
        private readonly IToken _tokenService;
        private readonly IAuthCookieFactory _cookieFactory;
        private readonly JwtSettings _jwt;
        private readonly CookieSettings _cookieSettings;

        public AuthController(
            IAuthService authService,
            IToken tokenService,
            IAuthCookieFactory cookieFactory,
            IOptions<JwtSettings> jwtOptions,
            IOptions<CookieSettings> cookieOptions,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _tokenService = tokenService;
            _cookieFactory = cookieFactory;
            _jwt = jwtOptions.Value;
            _cookieSettings = cookieOptions.Value;
            _logger = logger;
        }

        /// <summary>Login: genera access + refresh + csrf, guarda cookies HttpOnly.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            // Guardar tokens en cookies seguras (HTTP-only + SameSite)
            if (result.RequiresTwoFactor)
            {
                _logger.LogInformation("Usuario {Email} requiere verificación adicional via 2FA.", dto.Email);
                return Ok(new
                {
                    isSuccess = true,
                    message = "Se ha enviado un código de verificación al correo.",
                    requiresTwoFactor = true,
                    challenge = result.Challenge
                });
            }

            var tokens = result.Tokens ?? throw new InvalidOperationException("Se esperaban tokens de autenticación.");

            var issuedAt = WriteAuthCookies(tokens);

            _logger.LogInformation("Usuario {Email} autenticado correctamente y cookies emitidas.", dto.Email);

            return Ok(new
            {
                isSuccess = true,
                message = "Inicio de sesión exitoso",
                expiresAt = issuedAt.AddMinutes(_jwt.AccessTokenExpirationMinutes)
            });
        }


        /// <summary>Registra un usuario con contrasena temporal enviada por correo.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            _logger.LogInformation("Usuario {Email} registrado con roles [{Roles}].", result.Email, result.RoleIds);
            return Ok(new
            {
                isSuccess = true,
                message = "Usuario registrado correctamente. Revisa el correo para la contrasena temporal.",
                data = result
            });
        }

        /// <summary>Renueva tokens (usa refresh cookie + comprobación CSRF double-submit).</summary>

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenRefreshResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Refresh()
        {
            // 1️⃣ Validar existencia de cookies
            var refreshCookie = Request.Cookies[_cookieSettings.RefreshTokenName];
            if (string.IsNullOrWhiteSpace(refreshCookie))
                return Unauthorized("No se encontró el refresh token.");

            // 2️⃣ Validar token CSRF (double submit cookie pattern)
            if (!Request.Headers.TryGetValue("X-XSRF-TOKEN", out var headerValue))
                return Forbid("Falta el encabezado CSRF.");

            var csrfCookie = Request.Cookies[_cookieSettings.CsrfCookieName];
            if (string.IsNullOrWhiteSpace(csrfCookie) || csrfCookie != headerValue)
                return Forbid("CSRF token inválido o ausente.");

            // 3️⃣ Preparar DTO para el servicio
            var dto = new TokenRefreshRequestDto
            {
                RefreshToken = refreshCookie,
                RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            // 4️⃣ Llamar al servicio
            var result = await _tokenService.RefreshAsync(dto);

            var now = DateTime.UtcNow;

            // 5️⃣ Eliminar cookies previas (rotación)
            Response.Cookies.Delete(_cookieSettings.AccessTokenName, _cookieFactory.AccessCookieOptions(now));
            Response.Cookies.Delete(_cookieSettings.RefreshTokenName, _cookieFactory.RefreshCookieOptions(now));

            // 6️⃣ Reasignar nuevas cookies
            Response.Cookies.Append(
                _cookieSettings.AccessTokenName,
                result.AccessToken,
                _cookieFactory.AccessCookieOptions(now.AddMinutes(_jwt.AccessTokenExpirationMinutes)));

            Response.Cookies.Append(
                _cookieSettings.RefreshTokenName,
                result.RefreshToken,
                _cookieFactory.RefreshCookieOptions(now.AddDays(_jwt.RefreshTokenExpirationDays)));

            _logger.LogInformation("Tokens refrescados correctamente para la IP {Ip}.", dto.RemoteIp);

            // 7️⃣ Retornar respuesta tipada
            return Ok(new
            {
                isSuccess = true,
                message = "Tokens refrescados correctamente.",
                expiresAt = result.ExpiresAt
            });
        }


        /// <summary>Logout: revoca refresh token y borra cookies.</summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var refreshCookie = Request.Cookies[_cookieSettings.RefreshTokenName];
            if (!string.IsNullOrWhiteSpace(refreshCookie))
            {
                await _tokenService.RevokeRefreshTokenAsync(refreshCookie);
            }

            var now = DateTime.UtcNow;
            Response.Cookies.Delete(_cookieSettings.AccessTokenName, _cookieFactory.AccessCookieOptions(now));
            Response.Cookies.Delete(_cookieSettings.RefreshTokenName, _cookieFactory.RefreshCookieOptions(now));
            Response.Cookies.Delete(_cookieSettings.CsrfCookieName, _cookieFactory.CsrfCookieOptions(now));

            return Ok(new { message = "Sesión cerrada" });
        }

        /// <summary>/me: retorna el contexto del usuario actual.</summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                return Unauthorized("Token inválido o expirado.");

            var currentUserDto = await _authService.BuildUserContextAsync(userId);
            return Ok(currentUserDto);
        }

        /// <summary>Confirma el código de verificación y emite las cookies de autenticación (v1).</summary>
        [HttpPost("confirmar-2fa")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmarDosFactores([FromBody] TwoFactorVerifyDto dto)
        {
            var tokens = await _authService.ConfirmTwoFactorAsync(dto);
            var issuedAt = WriteAuthCookies(tokens);

            _logger.LogInformation("Código 2FA confirmado para {Email}.", dto.Email);

            return Ok(new
            {
                isSuccess = true,
                message = "Código de verificación confirmado.",
                expiresAt = issuedAt.AddMinutes(_jwt.AccessTokenExpirationMinutes),
                data = tokens
            });
        }

        /// <summary>Reenvía el código de 2FA cuando no llegó el correo inicial.</summary>
        [HttpPost("reenviar-2fa")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TwoFactorChallengeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReenviarCodigoDosFactores([FromBody] TwoFactorResendDto dto)
        {
            var challenge = await _authService.ResendTwoFactorCodeAsync(dto.Email);
            return Ok(new
            {
                isSuccess = true,
                message = "Se reenviará el código de verificación si el usuario está habilitado.",
                challenge
            });
        }

        /// <summary>Activa o desactiva el segundo factor para la cuenta actual (v1).</summary>
        [Authorize]
        [HttpPost("two-factor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetTwoFactorState([FromBody] TwoFactorToggleDto dto)
        {
            if (!TryResolveTargetUserId(dto, out var userId))
                return BadRequest("No se pudo detectar el usuario objetivo para la configuración de 2FA.");

            await _authService.ToggleTwoFactorAsync(userId, dto.Enabled);

            var stateMessage = dto.Enabled ? "Verificación en dos pasos activada." : "Verificación en dos pasos desactivada.";
            return Ok(new { isSuccess = true, message = stateMessage, enabled = dto.Enabled });
        }


        [HttpPost("recuperar/enviar-codigo")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EnviarCodigoAsync([FromBody] RequestResetDto dto)
        {
            await _authService.RequestPasswordResetAsync(dto.Email);
            return Ok(new { isSuccess = true, message = "Código enviado al correo (si el email es válido)" });
        }

        [HttpPost("recuperar/confirmar")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmarCodigo([FromBody] ConfirmResetDto dto)
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { isSuccess = true, message = "Contraseña actualizada" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            await _authService.ChangePasswordAsync(dto);
            return Ok(new { message = "Contraseña actualizada correctamente." });
        }
        
        private DateTime WriteAuthCookies(TokenResponseDto tokens)
        {
            var now = DateTime.UtcNow;

            Response.Cookies.Append(
                _cookieSettings.AccessTokenName,
                tokens.AccessToken,
                _cookieFactory.AccessCookieOptions(now.AddMinutes(_jwt.AccessTokenExpirationMinutes)));

            Response.Cookies.Append(
                _cookieSettings.RefreshTokenName,
                tokens.RefreshToken,
                _cookieFactory.RefreshCookieOptions(now.AddDays(_jwt.RefreshTokenExpirationDays)));

            Response.Cookies.Append(
                _cookieSettings.CsrfCookieName,
                tokens.CsrfToken,
                _cookieFactory.CsrfCookieOptions(now.AddDays(_jwt.RefreshTokenExpirationDays)));

            return now;
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claimValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claimValue, out userId);
        }

        private bool TryResolveTargetUserId(TwoFactorToggleDto dto, out int userId)
        {
            userId = 0;
            if (dto.UserId.HasValue)
            {
                userId = dto.UserId.Value;
                return true;
            }

            return TryGetUserId(out userId);
        }
    }
}



