using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Invoices;

public class InvoiceDto
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public Guid? QuoteId { get; set; }

    public InvoiceStatus Status { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime DueDate { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string? TaxName { get; set; }

    public decimal TaxRate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal BalanceDue { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<InvoiceItemDto> Items { get; set; }
        = new();
}