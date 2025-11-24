namespace Entity.DTOs.Implements.Payments;

public class MercadoPagoPreferenceResult
{
    public required string InitPoint { get; set; }
    public required string PreferenceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "COP";
    public int ObligationId { get; set; }
    public int ContractId { get; set; }
    public string? PaymentId { get; set; }
}
