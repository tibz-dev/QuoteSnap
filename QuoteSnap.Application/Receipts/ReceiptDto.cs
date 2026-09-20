using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Receipts;

public class ReceiptDto
{
    public Guid Id { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid PaymentId { get; set; }

    public Guid InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public DateTime ReceiptDate { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? PaymentReference { get; set; }

    public string? PaymentNotes { get; set; }

    public DateTime CreatedAt { get; set; }
}