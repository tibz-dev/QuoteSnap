using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Invoices;

public class PaymentDto
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}