using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Subscriptions;

public class InitializeSubscriptionPaymentRequest
{
    public SubscriptionPlan Plan { get; set; }
}