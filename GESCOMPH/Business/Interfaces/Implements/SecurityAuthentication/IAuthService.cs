using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Entity.DTOs.Implements.SecurityAuthentication.Auth.RestPasword;
using Entity.DTOs.Implements.SecurityAuthentication.Me;
using Entity.DTOs.Implements.SecurityAuthentication.User;

namespace Business.Interfaces.Implements.SecurityAuthentication
{
    public interface IAuthService
    {

        Task<LoginResultDto> LoginAsync(LoginDto dto);
        Task RequestPasswordResetAsync(string email);
        Task<UserMeDto> BuildUserContextAsync(int userId);
        Task ResetPasswordAsync(ConfirmResetDto dto);
        Task ChangePasswordAsync(ChangePasswordDto dto);
        Task<RegisterResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
        Task<RegisterResultDto> RegisterInternalAsync(RegisterDto dto, CancellationToken ct = default);
        Task ToggleTwoFactorAsync(int userId, bool enabled);
        Task<TokenResponseDto> ConfirmTwoFactorAsync(TwoFactorVerifyDto dto);
        Task<TwoFactorChallengeDto> ResendTwoFactorCodeAsync(string email);
    }
}
