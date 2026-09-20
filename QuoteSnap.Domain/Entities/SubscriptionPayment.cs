using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class SubscriptionPayment : TenantEntity
{
    public Guid SubscriptionId { get; set; }

    public SubscriptionPlan Plan { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public SubscriptionPaymentStatus Status { get; set; }
        = SubscriptionPaymentStatus.Pending;

    public string? PaymentProvider { get; set; }

    public string? ExternalPaymentId { get; set; }

    public string? ExternalReference { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public string? FailureReason { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public Business Business { get; set; } = null!;
}