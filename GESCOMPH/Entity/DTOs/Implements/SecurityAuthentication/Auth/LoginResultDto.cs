namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class LoginResultDto
    {
        public bool RequiresTwoFactor { get; set; }
        public TokenResponseDto? Tokens { get; set; }
        public TwoFactorChallengeDto? Challenge { get; set; }

        public static LoginResultDto FromTokens(TokenResponseDto tokens) => new()
        {
            RequiresTwoFactor = false,
            Tokens = tokens
        };

        public static LoginResultDto FromChallenge(TwoFactorChallengeDto challenge) => new()
        {
            RequiresTwoFactor = true,
            Challenge = challenge
        };
    }
}
