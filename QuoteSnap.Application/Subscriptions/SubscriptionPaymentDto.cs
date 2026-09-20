using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Subscriptions;

public class SubscriptionPaymentDto
{
    public Guid Id { get; set; }

    public SubscriptionPlan Plan { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public SubscriptionPaymentStatus Status { get; set; }

    public string? Reference { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }
}