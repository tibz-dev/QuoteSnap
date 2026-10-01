using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Subscriptions;

public class SubscriptionPlanDefinitionDto
{
    public SubscriptionPlan Plan { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public int? CustomerLimit { get; set; }

    public int? CatalogueItemLimit { get; set; }

    public int? MonthlyQuoteLimit { get; set; }

    public int? MonthlyInvoiceLimit { get; set; }

    public int? MonthlyReceiptLimit { get; set; }

    public bool EmailDelivery { get; set; }

    public bool PdfDocuments { get; set; } = true;

    public bool PaymentsAndReceipts { get; set; } = true;
}
