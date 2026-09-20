namespace QuoteSnap.Application.Quotes;

public class CreateQuoteItemRequest
{
    public Guid? CatalogueItemId { get; set; }

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }
}