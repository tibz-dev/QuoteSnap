namespace QuoteSnap.Application.Subscriptions;

public class SubscriptionOverviewDto
{
    public SubscriptionDto Subscription { get; set; } = new();

    public SubscriptionPlanDefinitionDto EffectivePlan { get; set; } = new();

    public List<SubscriptionPlanDefinitionDto> Plans { get; set; } = new();

    public SubscriptionUsageDto Usage { get; set; } = new();

    public bool CanWrite { get; set; }

    public bool IsReadOnly { get; set; }

    public bool IsTrialUsingProAccess { get; set; }

    public string? AccessMessage { get; set; }
}
