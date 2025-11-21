using System.Text;
using System.Text.Json;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.DTOs.Implements.Payments;
using Entity.Infrastructure.Configurations.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Services.Business.Payments
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly MercadoPagoSettings _settings;
        private readonly ILogger<MercadoPagoService> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public string WebhookSecret => _settings.WebhookSecret;

        public MercadoPagoService(
            HttpClient httpClient,
            IOptions<MercadoPagoSettings> settings,
            ILogger<MercadoPagoService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        // ============================================================
        // CREATE CHECKOUT PRO — con external_reference obligatorio
        // ============================================================
        public async Task<MercadoPagoPreferenceResult> CreateCheckoutPreferenceAsync(
            ObligationMonthSelectDto obligation,
            ContractSelectDto contract,
            string? payerEmail = null,
            string? payerDocument = null)
        {
            var payer = new
            {
                email = payerEmail ?? contract.Email,
                name = contract.FullName,
                identification = string.IsNullOrWhiteSpace(payerDocument)
                    ? null
                    : new { type = "CC", number = payerDocument }
            };

            var payload = new
            {
                items = new[]
                {
                    new
                    {
                        title = $"Contrato #{contract.Id:D6} - Obligación #{obligation.Id:D6}",
                        quantity = 1,
                        unit_price = (int)Math.Round(obligation.TotalAmount, MidpointRounding.AwayFromZero),
                        currency_id = "COP"
                    }
                },

                payer,

                back_urls = new
                {
                    success = _settings.SuccessUrl,
                    failure = _settings.FailureUrl,
                    pending = _settings.PendingUrl
                },

                auto_return = "approved",

                notification_url = _settings.NotificationUrl,

                // 🔥 CLAVE: para asociar el pago con la obligación
                external_reference = obligation.Id.ToString()
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/checkout/preferences", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MercadoPago error creando preferencia: {Error}", responseText);
                throw new HttpRequestException(
                    $"MercadoPago error {response.StatusCode}: {responseText}");
            }

            var json = JsonDocument.Parse(responseText).RootElement;

            return new MercadoPagoPreferenceResult
            {
                PreferenceId = json.GetProperty("id").GetString()!,
                InitPoint = json.GetProperty("init_point").GetString()!,
                Amount = obligation.TotalAmount,
                Currency = _settings.DefaultCurrency,
                ObligationId = obligation.Id,
                ContractId = contract.Id
            };
        }

        // ============================================================
        // GET PAYMENT INFO
        // ============================================================
        public async Task<MercadoPagoPaymentInfoDto?> GetPaymentAsync(string paymentId)
        {
            var response = await _httpClient.GetAsync($"/v1/payments/{paymentId}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Lookup de pago {PaymentId} falló: {Body}", paymentId, body);
                return null;
            }

            var json = JsonDocument.Parse(body).RootElement;

            return new MercadoPagoPaymentInfoDto
            {
                Id = json.TryGetProperty("id", out var id)
                    ? id.ValueKind == JsonValueKind.String
                        ? id.GetString()
                        : id.GetRawText().Trim('"')
                    : null,

                Status = json.TryGetProperty("status", out var status)
                    ? status.GetString()
                    : null,

                TransactionAmount = json.TryGetProperty("transaction_amount", out var amt)
                    ? amt.GetDecimal()
                    : null,

                ExternalReference = json.TryGetProperty("external_reference", out var ex)
                    ? (ex.ValueKind == JsonValueKind.String ? ex.GetString() : ex.GetRawText().Trim('"'))
                    : null
            };
        }


        // ============================================================
        // WEBHOOK SIGNATURE VALIDATION
        // ============================================================
        public bool ValidateWebhookSignature(string? signature, string payload)
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
                return true;

            return signature == _settings.WebhookSecret;
        }
    }
}
