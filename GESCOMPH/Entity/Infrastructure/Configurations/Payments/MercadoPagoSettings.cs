namespace Entity.Infrastructure.Configurations.Payments;

public class MercadoPagoSettings
{
    public string? AccessToken { get; set; }
    public string DefaultCurrency { get; set; } = "COP";
    public string BaseUrl { get; set; } = "https://api.mercadopago.com";
    public string? PublicKey { get; set; }
    public string? WebhookSecret { get; set; }
    public bool Sandbox { get; set; } = true;
    public string? SuccessUrl { get; set; }
    public string? FailureUrl { get; set; }
    public string? PendingUrl { get; set; }
    public string? NotificationUrl { get; set; }
}
