using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using Utilities.Messaging.Interfaces;

namespace Utilities.Messaging.Implements
{
    /// <summary>
    /// Implementación basada en SMTP real (antes: EmailService).
    /// </summary>
    public class SmtpEmailService : ISendCode
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService>? _logger;

        private const string BrandName = "GESCOMPH";
        private const string BrandPrimary = "#2E7D32";
        private const string BrandAccent = "#16a34a";
        private const string BrandText = "#1f2937";
        private const string BrandMuted = "#6b7280";
        private const string BrandBorder = "#e5e7eb";

        // Colores para estados críticos
        private const string ColorWarning = "#c2410c"; // Naranja oscuro para Pre-Jurídico
        private const string ColorDanger = "#b91c1c";  // Rojo para Jurídico

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService>? logger = null)
        {
            _config = config;
            _logger = logger;
        }

        private static void EnsureValidEmail(string? email, string paramName)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"El email '{paramName}' no puede ser nulo o vacío.", paramName);

            try { _ = new MailAddress(email); }
            catch
            {
                throw new ArgumentException($"El email '{paramName}' no tiene un formato válido.", paramName);
            }
        }

        private (string FromEmail, string Password, string Host, int Port, bool EnableSsl) LoadSmtpConfig()
        {
            var emailEmisor = _config["CONFIGURACIONES_EMAIL:EMAIL"];
            var password = _config["CONFIGURACIONES_EMAIL:PASSWORD"];
            var host = _config["CONFIGURACIONES_EMAIL:HOST"];
            var puertoStr = _config["CONFIGURACIONES_EMAIL:PUERTO"];
            var enableSslStr = _config["CONFIGURACIONES_EMAIL:ENABLE_SSL"];

            if (string.IsNullOrWhiteSpace(emailEmisor))
                throw new InvalidOperationException("CONFIGURACIONES_EMAIL:EMAIL no está configurado.");
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("CONFIGURACIONES_EMAIL:HOST no está configurado.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("CONFIGURACIONES_EMAIL:PASSWORD no está configurado.");
            if (!int.TryParse(puertoStr, out var puerto) || puerto <= 0)
                throw new InvalidOperationException("CONFIGURACIONES_EMAIL:PUERTO no es un entero válido (> 0).");

            var enableSsl = true;
            if (!string.IsNullOrWhiteSpace(enableSslStr) && bool.TryParse(enableSslStr, out var parsed))
                enableSsl = parsed;

            EnsureValidEmail(emailEmisor, "CONFIGURACIONES_EMAIL:EMAIL");

            return (emailEmisor!, password!, host!, puerto, enableSsl);
        }

        private async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            EnsureValidEmail(to, nameof(to));
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("El asunto no puede ser nulo o vacío.", nameof(subject));
            if (string.IsNullOrWhiteSpace(htmlBody))
                throw new ArgumentException("El cuerpo del correo no puede ser nulo o vacío.", nameof(htmlBody));

            var (fromEmail, password, host, port, enableSsl) = LoadSmtpConfig();

            using var smtpCliente = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail, password)
            };

            var displayName = $"{BrandName} • Soporte";
            var from = new MailAddress(fromEmail, displayName, Encoding.UTF8);
            var toAddr = new MailAddress(to);

            using var mensaje = new MailMessage(from, toAddr)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            try
            {
                await smtpCliente.SendMailAsync(mensaje);
                _logger?.LogInformation("Email enviado a {To} con asunto {Subject}", to, subject);
            }
            catch (SmtpException ex)
            {
                _logger?.LogError(ex, "SMTP error enviando correo a {To} (host {Host}:{Port})", to, host, port);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error general enviando correo a {To}", to);
                throw;
            }
        }

        private string WrapEmail(string title, string contentHtml)
        {
            var logoUrl = _config["APP:LOGO_URL"];

            var logoHtml = !string.IsNullOrWhiteSpace(logoUrl)
                ? $"<img src='{logoUrl}' alt='{BrandName}' style='height:40px; display:block;'/>"
                : $"<strong style='font-size:18px; color:white; letter-spacing:.5px'>{BrandName}</strong>";

            return $@"
                <!DOCTYPE html>
                <html lang='es'>
                <head>
                  <meta charset='UTF-8'>
                  <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
                  <title>{title}</title>
                </head>
                <body style='margin:0; padding:24px; background:#f8fafc; font-family:Segoe UI, Roboto, Arial, sans-serif;'>
                  <div style='max-width:640px; margin:0 auto;'>
                    <div style='background:{BrandPrimary}; padding:16px 20px; border-radius:12px 12px 0 0;'>
                      {logoHtml}
                    </div>
                    <div style='background:white; border:1px solid {BrandBorder}; border-top:none; padding:28px; border-radius:0 0 12px 12px; box-shadow:0 6px 24px rgba(0,0,0,.06);'>
                      <h2 style='margin:0 0 12px 0; color:{BrandText}; font-size:20px;'>{title}</h2>
                      <div style='color:{BrandText}; line-height:1.6; font-size:15px;'>{contentHtml}</div>
                      <hr style='border:none; border-top:1px solid {BrandBorder}; margin:24px 0'/>
                      <p style='margin:0; color:{BrandMuted}; font-size:12px;'>
                        Este mensaje fue enviado por {BrandName}. Si no reconoces esta solicitud, ignora este correo.
                      </p>
                    </div>
                  </div>
                </body>
                </html>";
        }

        public async Task SendRecoveryCodeEmail(string emailReceptor, string recoveryCode)
        {
            EnsureValidEmail(emailReceptor, nameof(emailReceptor));

            var content = $@"
                        <p>Hemos recibido una solicitud para <strong>restablecer tu contraseña</strong>.</p>
                        <p>Tu código de verificación es:</p>
                        <div style='display:inline-block; padding:12px 18px; font-size:26px; font-weight:700; letter-spacing:2px; background:{BrandAccent}; color:white; border-radius:10px;'>
                            {recoveryCode}
                        </div>
                        <p style='margin-top:16px; color:{BrandMuted}; font-size:13px;'>
                            Este código es válido por <strong>10 minutos</strong>.
                        </p>";

            var html = WrapEmail("Recuperación de contraseña", content);
            await SendEmailAsync(emailReceptor, "GESCOMPH - Recuperación de contraseña", html);
        }

        public async Task SendTwoFactorCodeEmailAsync(string emailReceptor, string verificationCode, int validityMinutes, string subject)
        {
            EnsureValidEmail(emailReceptor, nameof(emailReceptor));

            if (string.IsNullOrWhiteSpace(verificationCode))
                throw new ArgumentException("El código de verificación no puede estar vacío.", nameof(verificationCode));

            var safeSubject = string.IsNullOrWhiteSpace(subject) ? "Código de verificación" : subject.Trim();
            var validity = Math.Max(1, validityMinutes);

            var content = $@"
                        <p>Tu código de verificación en <strong>{BrandName}</strong> es:</p>
                        <div style='display:inline-block; padding:12px 18px; font-size:26px; font-weight:700; letter-spacing:2px; background:{BrandAccent}; color:white; border-radius:10px;'>
                            {verificationCode}
                        </div>
                        <p style='margin-top:16px; color:{BrandMuted}; font-size:13px;'>
                            Este código es válido por <strong>{validity} minutos</strong>.
                        </p>";

            var html = WrapEmail(safeSubject, content);
            await SendEmailAsync(emailReceptor, $"GESCOMPH - {safeSubject}", html);
        }

        public async Task SendTemporaryPasswordAsync(string email, string fullName, string tempPassword)
        {
            EnsureValidEmail(email, nameof(email));

            var content = $@"
                    <p>Hola <strong>{(fullName ?? "usuario")}</strong>, tu cuenta en <strong>{BrandName}</strong> ha sido creada exitosamente.</p>
                    <p>Tu <strong>contraseña temporal</strong> es:</p>
                    <div style='display:inline-block; padding:12px 18px; font-size:22px; font-weight:700; background:#111827; color:white; border-radius:10px;'>
                        {tempPassword}
                    </div>
                    <p style='margin-top:16px;'>Por seguridad, deberás cambiarla en tu <strong>primer ingreso</strong> al sistema.</p>";

            var html = WrapEmail("Tu cuenta fue creada", content);
            await SendEmailAsync(email, "GESCOMPH – Tu cuenta fue creada", html);
        }

        public async Task SendContractWithPdfAsync(string email, string fullName, string contractNumber, byte[] pdfBytes)
        {
            EnsureValidEmail(email, nameof(email));
            
            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new ArgumentException("El PDF del contrato no puede ser nulo o vacío.", nameof(pdfBytes));

            var content = $@"
                    <p>Estimado/a <strong>{(fullName ?? "usuario")}</strong>,</p>
                    <p>Nos complace informarle que su <strong>contrato de arrendamiento #{contractNumber}</strong> ha sido generado exitosamente.</p>
                    <p>Adjunto a este correo encontrará el documento PDF con todos los términos y condiciones acordados.</p>
                    <div style='background:#f8f9fa; border-left:4px solid {BrandPrimary}; padding:16px; margin:16px 0; border-radius:6px;'>
                        <p style='margin:0; font-weight:600; color:{BrandText};'>📄 Contrato #{contractNumber}</p>
                        <p style='margin:4px 0 0 0; color:{BrandMuted}; font-size:14px;'>Documento adjunto en formato PDF</p>
                    </div>
                    <p>Le recomendamos revisar cuidadosamente el documento y conservar una copia para sus registros.</p>
                    <p>Si tiene alguna pregunta o requiere aclaraciones, no dude en contactarnos.</p>";

            var html = WrapEmail("Contrato de Arrendamiento Generado", content);
            await SendEmailWithAttachmentAsync(email, "GESCOMPH – Contrato de Arrendamiento", html, pdfBytes, $"Contrato_{contractNumber}.pdf");
        }

        private async Task SendEmailWithAttachmentAsync(string to, string subject, string htmlBody, byte[] attachmentBytes, string attachmentName)
        {
            EnsureValidEmail(to, nameof(to));
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("El asunto no puede ser nulo o vacío.", nameof(subject));
            if (string.IsNullOrWhiteSpace(htmlBody))
                throw new ArgumentException("El cuerpo del correo no puede ser nulo o vacío.", nameof(htmlBody));

            var (fromEmail, password, host, port, enableSsl) = LoadSmtpConfig();

            using var smtpCliente = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail, password)
            };

            var displayName = $"{BrandName} • Contratos";
            var from = new MailAddress(fromEmail, displayName, Encoding.UTF8);
            var toAddr = new MailAddress(to);

            using var mensaje = new MailMessage(from, toAddr)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            // Agregar el PDF como adjunto
            if (attachmentBytes != null && attachmentBytes.Length > 0)
            {
                var attachment = new Attachment(new MemoryStream(attachmentBytes), attachmentName, "application/pdf");
                mensaje.Attachments.Add(attachment);
            }

            try
            {
                await smtpCliente.SendMailAsync(mensaje);
                _logger?.LogInformation("Email con adjunto PDF enviado a {To} con asunto {Subject}", to, subject);
            }
            catch (SmtpException ex)
            {
                _logger?.LogError(ex, "SMTP error enviando correo con adjunto a {To} (host {Host}:{Port})", to, host, port);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error general enviando correo con adjunto a {To}", to);
                throw;
            }
        }

        public async Task SendPaymentReminderAsync(string email, string fullName, DateTime dueDate, decimal totalAmount)
        {
            EnsureValidEmail(email, nameof(email));

            var formattedDate = dueDate.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-CO"));
            var formattedValue = totalAmount.ToString("N0", new System.Globalization.CultureInfo("es-CO"));

            var content = $@"
                <p>Hola <strong>{(fullName ?? "arrendatario")}</strong>,</p>
                <p>Te recordamos que tu <strong>canon de arrendamiento</strong> vence el <strong>{formattedDate}</strong>.</p>
                <div style='background:#f1f5f9; border-left:4px solid {BrandAccent}; padding:16px; margin:16px 0; border-radius:6px;'>
                    <p style='margin:0; font-weight:600; color:{BrandText};'>💰 Valor a pagar: ${formattedValue} COP</p>
                </div>
                <p>Por favor realiza el pago antes de la fecha de vencimiento para evitar intereses moratorios.</p>
                <p>Gracias por mantener tus obligaciones al día.</p>";

            var html = WrapEmail("Recordatorio de pago próximo a vencer", content);
            await SendEmailAsync(email, "GESCOMPH – Recordatorio de pago próximo a vencer", html);
        }

        public async Task SendOverdueNoticeAsync(string email, string fullName, DateTime dueDate, decimal totalAmount, int daysLate, decimal lateAmount)
        {
            EnsureValidEmail(email, nameof(email));

            var formattedDue = dueDate.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-CO"));
            var formattedTotal = totalAmount.ToString("N0", new System.Globalization.CultureInfo("es-CO"));
            var formattedLate = lateAmount.ToString("N0", new System.Globalization.CultureInfo("es-CO"));

            var content = $@"
                <p>Hola <strong>{(fullName ?? "arrendatario")}</strong>,</p>
                <p>Tu obligación de arrendamiento con fecha de vencimiento <strong>{formattedDue}</strong> se encuentra vencida.</p>
                <div style='background:#fff7ed; border-left:4px solid #f97316; padding:16px; margin:16px 0; border-radius:6px;'>
                    <p style='margin:0; font-weight:600; color:{BrandText};'>💰 Valor original: ${formattedTotal} COP</p>
                    <p style='margin:4px 0 0 0; color:{BrandMuted}; font-size:14px;'>Días de mora: {daysLate}</p>
                    <p style='margin:4px 0 0 0; color:{BrandMuted}; font-size:14px;'>Intereses acumulados: ${formattedLate} COP</p>
                </div>
                <p>Por favor regulariza tu pago lo antes posible para evitar procesos de cobro jurídico.</p>
                <p>Si ya realizaste el pago, ignora este mensaje.</p>";

            var html = WrapEmail("Notificación de obligación vencida", content);
            await SendEmailAsync(email, "GESCOMPH – Notificación de pago vencido", html);
        }

        public async Task SendPreJudicialNoticeAsync(string email, string fullName, decimal totalDebt, DateTime paymentDeadline)
        {
            EnsureValidEmail(email, nameof(email));

            var formattedDebt = totalDebt.ToString("N0", new System.Globalization.CultureInfo("es-CO"));
            var formattedDate = paymentDeadline.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-CO"));

            var content = $@"
                <p>Señor(a) <strong>{(fullName ?? "Arrendatario")}</strong>,</p>
                <p>Le informamos que su obligación presenta una mora superior a <strong>30 días</strong> y ha entrado en etapa de <strong>Cobro Pre-Jurídico</strong>.</p>
                
                <div style='background:#fff3e0; border-left:6px solid {ColorWarning}; padding:18px; margin:20px 0; border-radius:6px;'>
                    <h3 style='margin:0 0 8px 0; color:{ColorWarning}; font-size:18px;'>⚠️ Aviso de Cobro Pre-Jurídico</h3>
                    <p style='margin:4px 0; color:{BrandText}; font-size:16px;'><strong>Deuda Total Acumulada:</strong> ${formattedDebt} COP</p>
                    <p style='margin:4px 0; color:{BrandText}; font-size:16px;'><strong>Fecha Límite de Pago:</strong> {formattedDate}</p>
                </div>

                <p>Tiene un plazo perentorio de <strong>5 días calendario</strong> (hasta el {formattedDate}) para cancelar la totalidad de la deuda.</p>
                <p>De no recibir el pago en este plazo, su caso será trasladado automáticamente a nuestro <strong>Departamento Jurídico</strong> para iniciar las acciones legales correspondientes según el contrato de arrendamiento.</p>
                <p>Evite costos adicionales de abogados y reportes negativos.</p>";

            // Usamos el color de advertencia en el header
            var html = WrapEmail("AVISO URGENTE: Cobro Pre-Jurídico", content);

            await SendEmailAsync(email, "URGENTE: Notificación de Cobro Pre-Jurídico - GESCOMPH", html);
        }

        public async Task SendJudicialNoticeAsync(string email, string fullName)
        {
            EnsureValidEmail(email, nameof(email));

            var content = $@"
                <p>Señor(a) <strong>{(fullName ?? "Arrendatario")}</strong>,</p>
                <p>Hacemos de su conocimiento que, al no haber recibido el pago de sus obligaciones pendientes dentro del plazo otorgado en la etapa pre-jurídica, su contrato ha pasado a la etapa de <strong>Cobro Jurídico</strong>.</p>
                
                <div style='background:#fef2f2; border-left:6px solid {ColorDanger}; padding:18px; margin:20px 0; border-radius:6px;'>
                    <h3 style='margin:0 0 8px 0; color:{ColorDanger}; font-size:18px;'>⚖️ Notificación de Proceso Jurídico</h3>
                    <p style='margin:4px 0; color:{BrandText};'>Su expediente ha sido remitido a nuestros asesores legales externos.</p>
                </div>

                <p>A partir de este momento, cualquier comunicación o acuerdo de pago deberá realizarse directamente a través de la firma de abogados asignada.</p>
                <p>Esto implicará el cobro de honorarios profesionales y costas procesales adicionales a la deuda existente.</p>
                <p style='font-weight:bold; color:{ColorDanger};'>Esta es una notificación informativa final por parte del sistema de gestión.</p>";

            // Usamos el color de peligro (rojo) en el header
            var html = WrapEmail("NOTIFICACIÓN LEGAL: Inicio Proceso Jurídico", content);

            await SendEmailAsync(email, "NOTIFICACIÓN FINAL: Traslado a Cobro Jurídico - GESCOMPH", html);
        }
    }
}

