using QuoteSnap.Domain.Common;

namespace QuoteSnap.Domain.Entities;

public class Receipt : TenantEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid PaymentId { get; set; }

    public Guid InvoiceId { get; set; }

    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public Payment Payment { get; set; } = null!;

    public Invoice Invoice { get; set; } = null!;
}