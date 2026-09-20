namespace QuoteSnap.Application.Subscriptions;

public class InitializeSubscriptionPaymentResponse
{
    public Guid PaymentId { get; set; }

    public string Reference { get; set; } = string.Empty;

    public string AuthorizationUrl { get; set; } = string.Empty;

    public string AccessCode { get; set; } = string.Empty;
}