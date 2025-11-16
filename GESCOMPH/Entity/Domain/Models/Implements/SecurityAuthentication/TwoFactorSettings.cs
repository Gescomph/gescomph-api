using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Domain.Models.Implements.SecurityAuthentication
{
    public class TwoFactorSettings
    {
        /// <summary>
        /// Longitud del código numérico enviado por correo.
        /// </summary>
        public int CodeLength { get; set; } = 6;

        /// <summary>
        /// Minutos de vigencia del código.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 5;

        /// <summary>
        /// En segundos, el tiempo mínimo antes de volver a enviar un código nuevo.
        /// </summary>
        public int ResendCooldownSeconds { get; set; } = 60;

        /// <summary>
        /// Asunto del correo de verificación.
        /// </summary>
        public string EmailSubject { get; set; } = "Código de verificación";
    }
}
