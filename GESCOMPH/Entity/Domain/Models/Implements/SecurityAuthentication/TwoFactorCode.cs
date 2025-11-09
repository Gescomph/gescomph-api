using Entity.Domain.Models.ModelBase;

namespace Entity.Domain.Models.Implements.SecurityAuthentication
{
    /// <summary>
    /// Código temporal utilizado para la verificación de doble factor vía correo electrónico.
    /// </summary>
    public class TwoFactorCode : BaseModel
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Code { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
