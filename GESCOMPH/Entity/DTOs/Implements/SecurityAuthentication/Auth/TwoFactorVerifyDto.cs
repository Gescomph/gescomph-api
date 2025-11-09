namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class TwoFactorVerifyDto
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}
