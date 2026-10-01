namespace QuoteSnap.Application.Subscriptions;

public class SubscriptionUsageDto
{
    public int Customers { get; set; }

    public int CatalogueItems { get; set; }

    public int QuotesThisMonth { get; set; }

    public int InvoicesThisMonth { get; set; }

    public int ReceiptsThisMonth { get; set; }
}
