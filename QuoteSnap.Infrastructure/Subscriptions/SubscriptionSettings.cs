namespace QuoteSnap.Infrastructure.Subscriptions;

public class SubscriptionSettings
{
    public const string SectionName = "Subscriptions";

    public int TrialDays { get; set; } = 14;
}