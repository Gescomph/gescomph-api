namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class TwoFactorToggleDto
    {
        /// <summary>
        /// Indica si la verificación en dos pasos debe estar activada (true) o desactivada (false).
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Identificador del usuario sobre el que se aplica el cambio de estado.
        /// Si no se proporciona, se usa el usuario autenticado actual.
        /// </summary>
        public int? UserId { get; set; }
    }
}
