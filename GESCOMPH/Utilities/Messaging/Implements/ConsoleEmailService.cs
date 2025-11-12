using Microsoft.Extensions.Logging;
using Utilities.Messaging.Interfaces;

namespace Utilities.Messaging.Implements
{
    /// <summary>
    /// Implementación que sólo registra en logs. Útil en desarrollo o testing.
    /// </summary>
    public class ConsoleEmailService : ISendCode
    {
        private readonly ILogger<ConsoleEmailService> _logger;

        public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendRecoveryCodeEmail(string emailReceptor, string recoveryCode)
        {
            _logger.LogInformation("[ConsoleEmail] RecoveryCode -> {Email}: {Code}", emailReceptor, recoveryCode);
            return Task.CompletedTask;
        }

        public Task SendTemporaryPasswordAsync(string email, string fullName, string tempPassword)
        {
            _logger.LogInformation("[ConsoleEmail] TempPassword -> {Email} ({Name}): {Password}",
                email, fullName, tempPassword);
            return Task.CompletedTask;
        }

        public Task SendContractWithPdfAsync(string email, string fullName, string contractNumber, byte[] pdfBytes)
        {
            _logger.LogInformation("[ConsoleEmail] ContractPDF -> {Email} ({Name}): Contrato #{ContractNumber}, PDF Size: {PdfSize} bytes",
                email, fullName, contractNumber, pdfBytes?.Length ?? 0);
            return Task.CompletedTask;
        }

        public Task SendPaymentReminderAsync(string email, string fullName, DateTime dueDate, decimal totalAmount)
        {
            _logger.LogInformation("[ConsoleEmail] PaymentReminder -> {Email} ({Name}): Vence el {DueDate:dd/MM/yyyy}, Monto: {TotalAmount:C}",
                email, fullName, dueDate, totalAmount);
            return Task.CompletedTask;
        }

        public Task SendOverdueNoticeAsync(string email, string fullName, DateTime dueDate, decimal totalAmount, int daysLate, decimal lateAmount)
        {
            _logger.LogInformation("[ConsoleEmail] OverdueNotice -> {Email} ({Name}): Vencido el {DueDate:dd/MM/yyyy}, Monto: {Total:C}, Días Mora: {DaysLate}, Intereses: {LateAmount:C}",
                email, fullName, dueDate, totalAmount, daysLate, lateAmount);
            return Task.CompletedTask;
        }
    }
}
