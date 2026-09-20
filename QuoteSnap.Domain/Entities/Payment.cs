using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class Payment : TenantEntity
{
    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public Invoice Invoice { get; set; } = null!;
}