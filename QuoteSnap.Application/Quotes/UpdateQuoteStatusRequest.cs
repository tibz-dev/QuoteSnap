using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Quotes;

public class UpdateQuoteStatusRequest
{
    public QuoteStatus Status { get; set; }
}