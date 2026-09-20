namespace QuoteSnap.Application.Quotes;

public class QuoteItemDto
{
    public Guid Id { get; set; }

    public Guid? CatalogueItemId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineSubtotal { get; set; }

    public decimal LineTotal { get; set; }
}