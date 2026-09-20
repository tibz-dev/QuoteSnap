using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class Subscription : TenantEntity
{
    public SubscriptionPlan Plan { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime? TrialStartedAt { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    public DateTime? CurrentPeriodStartsAt { get; set; }

    public DateTime? CurrentPeriodEndsAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string? PaymentProvider { get; set; }

    public string? ExternalCustomerId { get; set; }

    public string? ExternalSubscriptionId { get; set; }

    public string? ExternalSubscriptionEmailToken { get; set; }

    public Business Business { get; set; } = null!;

    public ICollection<SubscriptionPayment> Payments { get; set; }
    = new List<SubscriptionPayment>();

    
}