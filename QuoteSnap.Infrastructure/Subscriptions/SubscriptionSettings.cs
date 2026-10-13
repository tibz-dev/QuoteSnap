namespace QuoteSnap.Infrastructure.Subscriptions;

public class SubscriptionSettings
{
    public const string SectionName = "Subscriptions";

    public int TrialDays { get; set; } = 14;

    public decimal ProMonthlyPrice { get; set; } = 99;

    public decimal BusinessMonthlyPrice { get; set; } = 199;

    public string BillingCurrency { get; set; } = "ZAR";

    public int FreeCustomerLimit { get; set; } = 5;

    public int FreeCatalogueItemLimit { get; set; } = 10;

    public int FreeMonthlyQuoteLimit { get; set; } = 5;

    public int FreeMonthlyInvoiceLimit { get; set; } = 5;

    public int FreeMonthlyReceiptLimit { get; set; } = 10;

    public int ProCustomerLimit { get; set; } = 100;

    public int ProCatalogueItemLimit { get; set; } = 250;

    public int ProMonthlyQuoteLimit { get; set; } = 100;

    public int ProMonthlyInvoiceLimit { get; set; } = 100;

    public int ProMonthlyReceiptLimit { get; set; } = 250;

    public bool TrialUsesProAccess { get; set; } = true;
}
