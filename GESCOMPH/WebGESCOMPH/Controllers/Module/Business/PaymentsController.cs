using Business.Interfaces.Implements.Business;
using Business.Services.Business.Payments;
using Entity.DTOs.Implements.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace WebGESCOMPH.Controllers.Module.Business;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IContractService _contractService;
    private readonly IObligationMonthService _obligationService;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly ILogger<PaymentsController> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public PaymentsController(
        IContractService contractService,
        IObligationMonthService obligationService,
        IMercadoPagoService mercadoPagoService,
        ILogger<PaymentsController> logger)
    {
        _contractService = contractService;
        _obligationService = obligationService;
        _mercadoPagoService = mercadoPagoService;
        _logger = logger;
    }

    // ============================================================
    //   ENDPOINTS DE CHECKOUT
    // ============================================================
    [HttpPost("contracts/{contractId:int}/obligations/{obligationId:int}/checkout")]
    public async Task<IActionResult> CreateCheckout(int contractId, int obligationId)
    {
        var contract = await _contractService.GetByIdAsync(contractId);
        if (contract == null)
            return NotFound(new { message = $"Contrato {contractId} no encontrado." });

        var obligation = await _obligationService.GetByIdAsync(obligationId);
        if (obligation == null || obligation.ContractId != contract.Id)
            return NotFound(new { message = $"Obligación {obligationId} no encontrada para el contrato {contractId}." });

        var pref = await _mercadoPagoService.CreateCheckoutPreferenceAsync(
            obligation,
            contract,
            payerEmail: contract.Email,
            payerDocument: contract.Document
        );

        return Ok(new { url = pref.InitPoint });
    }

    [HttpPost("obligations/{obligationId:int}/checkout")]
    public async Task<IActionResult> CheckoutObligation(int obligationId)
    {
        var obligation = await _obligationService.GetByIdAsync(obligationId);
        if (obligation == null)
            return NotFound(new { message = $"Obligación {obligationId} no encontrada." });

        var contract = await _contractService.GetByIdAsync(obligation.ContractId);
        if (contract == null)
            return NotFound(new { message = $"Contrato {obligation.ContractId} no encontrado." });

        var pref = await _mercadoPagoService.CreateCheckoutPreferenceAsync(
            obligation,
            contract,
            payerEmail: contract.Email,
            payerDocument: contract.Document
        );

        return Ok(new { url = pref.InitPoint });
    }

    // ============================================================
    //   WEBHOOK OFICIAL DE MERCADO PAGO
    [HttpPost("mercadopago/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        _logger.LogInformation("Webhook recibido: {Payload}", rawPayload);

        // 🔥 1. Parseo dinámico, no usamos DTO
        using var json = JsonDocument.Parse(rawPayload);
        var root = json.RootElement;

        string? paymentId = null;

        // Forma 1: {"resource": "134552..."}
        if (root.TryGetProperty("resource", out var resProp))
            paymentId = resProp.GetString();

        // Forma 2: {"data": {"id": "134552..."}}
        if (paymentId == null && root.TryGetProperty("data", out var dataProp))
        {
            if (dataProp.TryGetProperty("id", out var idProp))
                paymentId = idProp.GetString();
        }

        // Forma 3: {"id": "134..." }
        if (paymentId == null && root.TryGetProperty("id", out var idRoot))
            paymentId = idRoot.GetRawText().Trim('"');

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            _logger.LogWarning("Webhook sin paymentId válido.");
            return Ok();
        }

        // 🔥 2. Consultar pago real
        var payment = await _mercadoPagoService.GetPaymentAsync(paymentId);
        if (payment == null)
        {
            _logger.LogWarning("No se encontró pago {PaymentId}.", paymentId);
            return Ok();
        }

        if (!string.Equals(payment.Status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Pago {PaymentId} no aprobado ({Status}). Ignorado.", paymentId, payment.Status);
            return Ok();
        }

        if (string.IsNullOrWhiteSpace(payment.ExternalReference))
        {
            _logger.LogWarning("Pago {PaymentId} sin external_reference.", paymentId);
            return Ok();
        }

        if (!int.TryParse(payment.ExternalReference, out var obligationId))
        {
            _logger.LogWarning("external_reference inválido: {Ref}", payment.ExternalReference);
            return Ok();
        }

        var obligation = await _obligationService.GetByIdAsync(obligationId);
        if (obligation == null)
        {
            _logger.LogWarning("Obligación {ObligationId} no existe.", obligationId);
            return Ok();
        }

        await _obligationService.MarkAsPaidAsync(obligation.Id);

        _logger.LogInformation("✔ Obligación {ObligationId} marcada como PAGADA.", obligationId);

        return Ok();
    }

}
