using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Subscriptions;

public class SubscriptionDto
{
    public Guid Id { get; set; }

    public SubscriptionPlan Plan { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime? TrialStartedAt { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    public int? TrialDaysRemaining { get; set; }

    public DateTime? CurrentPeriodStartsAt { get; set; }

    public DateTime? CurrentPeriodEndsAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? EndedAt { get; set; }
}