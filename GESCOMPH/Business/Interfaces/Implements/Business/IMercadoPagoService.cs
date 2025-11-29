using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.DTOs.Implements.Payments;

namespace Business.Services.Business.Payments;

public interface IMercadoPagoService
{
    // Crear una preferencia (Checkout Pro)
    Task<MercadoPagoPreferenceResult> CreateCheckoutPreferenceAsync(
        ObligationMonthSelectDto obligation,
        ContractSelectDto contract,
        string? payerEmail = null,
        string? payerDocument = null);

    // Obtener información de un pago
    Task<MercadoPagoPaymentInfoDto?> GetPaymentAsync(string paymentId);

    // Validar firma del webhook (si aplica)
    bool ValidateWebhookSignature(string? signature, string payload);

    // Propiedades para que el controller pueda usarlas
    string WebhookSecret { get; }
}
