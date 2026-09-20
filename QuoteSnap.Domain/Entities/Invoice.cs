using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class Invoice : TenantEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public Guid? QuoteId { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    // Currency snapshot
    public string CurrencyCode { get; set; } = string.Empty;

    // Tax snapshot
    public string? TaxName { get; set; }

    public decimal TaxRate { get; set; }

    // Financial values
    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public decimal AmountPaid { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    // Navigation
    public Business Business { get; set; } = null!;

    public Customer Customer { get; set; } = null!;

    public Quote? Quote { get; set; }

    public ICollection<InvoiceItem> Items { get; set; }
        = new List<InvoiceItem>();

    public decimal BalanceDue => Total - AmountPaid;

    public void CalculateTotals()
    {
        foreach (var item in Items)
        {
            item.CalculateTotal();
        }

        Subtotal = Items.Sum(x => x.LineSubtotal);

        DiscountAmount = Items.Sum(x => x.DiscountAmount);

        var taxableAmount = Subtotal - DiscountAmount;

        TaxAmount = taxableAmount * (TaxRate / 100m);

        Total = taxableAmount + TaxAmount;
    }
}