namespace Entity.DTOs.Implements.Payments;

public class MercadoPagoWebhookPayload
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public MercadoPagoWebhookData? Data { get; set; }
}

public class MercadoPagoWebhookData
{
    public MercadoPagoWebhookDataBody? Body { get; set; }
    public MercadoPagoWebhookDataId? Id { get; set; }
}

public class MercadoPagoWebhookDataBody
{
    public string? Id { get; set; }
    public string? Type { get; set; }
}

public class MercadoPagoWebhookDataId
{
    public string? Id { get; set; }
    public string? Type { get; set; }
}

public class MercadoPagoPaymentInfoDto
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public decimal? TransactionAmount { get; set; }
    public MercadoPagoPaymentMetadata? Metadata { get; set; }
    public string? ExternalReference { get; set; }
}

public class MercadoPagoPaymentMetadata
{
    public int? ObligationId { get; set; }
    public int? ContractId { get; set; }
    public string? PersonName { get; set; }
    public string? PersonDocument { get; set; }
}
