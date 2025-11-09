namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class TwoFactorChallengeDto
    {
        public string Email { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string DeliveryChannel { get; set; } = "email";
    }
}
