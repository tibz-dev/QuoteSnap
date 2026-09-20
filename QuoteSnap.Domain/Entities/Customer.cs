using QuoteSnap.Domain.Common;

namespace QuoteSnap.Domain.Entities;

public class Customer : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public Business Business { get; set; } = null!;

    public ICollection<Quote> Quotes { get; set; }
    = new List<Quote>();

    public ICollection<Invoice> Invoices { get; set; }
        = new List<Invoice>();
}