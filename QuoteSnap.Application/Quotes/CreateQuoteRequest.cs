namespace QuoteSnap.Application.Quotes;

public class CreateQuoteRequest
{
    public Guid CustomerId { get; set; }

    public DateTime? ValidUntil { get; set; }

    public string? CurrencyCode { get; set; }

    public decimal? TaxRate { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public List<CreateQuoteItemRequest> Items { get; set; }
        = new();
}