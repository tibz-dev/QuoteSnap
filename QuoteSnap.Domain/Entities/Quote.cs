using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class Quote : TenantEntity
{
    public string QuoteNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    public DateTime ValidUntil { get; set; }

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

    public string? Notes { get; set; }

    public string? Terms { get; set; }

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

    // Navigation
    public Business Business { get; set; } = null!;

    public Customer Customer { get; set; } = null!;

    public ICollection<QuoteItem> Items { get; set; }
        = new List<QuoteItem>();
}