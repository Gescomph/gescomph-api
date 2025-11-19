using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Messaging.Interfaces
{
    public interface ISendCode
    {
        Task SendRecoveryCodeEmail(string emailReceptor, string recoveryCode);
        Task SendTemporaryPasswordAsync(string email, string fullName, string tempPassword);
        Task SendContractWithPdfAsync(string email, string fullName, string contractNumber, byte[] pdfBytes);
        Task SendTwoFactorCodeEmailAsync(string emailReceptor, string verificationCode, int validityMinutes, string subject);

        Task SendPaymentReminderAsync(string email, string fullName, DateTime dueDate, decimal totalAmount);
        Task SendOverdueNoticeAsync(string email, string fullName, DateTime dueDate, decimal totalAmount, int daysLate, decimal lateAmount);

        /// <summary>
        /// Notificación de etapa Pre-Jurídica (30+ días de mora).
        /// </summary>
        Task SendPreJudicialNoticeAsync(string email, string fullName, decimal totalDebt, DateTime paymentDeadline);

        /// <summary>
        /// Notificación de etapa Jurídica (Fin del plazo de gracia).
        /// </summary>
        Task SendJudicialNoticeAsync(string email, string fullName);
    }
}
