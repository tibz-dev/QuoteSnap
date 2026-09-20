namespace QuoteSnap.Application.Invoices;

public class SendInvoiceEmailRequest
{
    public string? To { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }
}