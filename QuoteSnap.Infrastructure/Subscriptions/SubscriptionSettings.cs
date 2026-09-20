namespace QuoteSnap.Infrastructure.Subscriptions;

public class SubscriptionSettings
{
    public const string SectionName = "Subscriptions";

    public int TrialDays { get; set; } = 14;

    public decimal ProMonthlyPrice { get; set; } = 99;

    public decimal BusinessMonthlyPrice { get; set; } = 199;

    public string BillingCurrency { get; set; } = "ZAR";
}