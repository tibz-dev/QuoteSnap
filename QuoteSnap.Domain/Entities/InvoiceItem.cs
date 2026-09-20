using QuoteSnap.Domain.Common;

namespace QuoteSnap.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }

    public Guid? CatalogueItemId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineSubtotal { get; set; }

    public decimal LineTotal { get; set; }

    public Invoice Invoice { get; set; } = null!;

    public CatalogueItem? CatalogueItem { get; set; }

    public void CalculateTotal()
    {
        LineSubtotal = Quantity * UnitPrice;

        LineTotal = LineSubtotal - DiscountAmount;
    }
}