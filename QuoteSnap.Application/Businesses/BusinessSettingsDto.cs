namespace QuoteSnap.Application.Businesses;

public class BusinessSettingsDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public bool IsTaxRegistered { get; set; }

    public string? TaxName { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public decimal? DefaultTaxRate { get; set; }

    public string? BankName { get; set; }

    public string? AccountHolder { get; set; }

    public string? AccountNumber { get; set; }

    public string? BranchCode { get; set; }

    public string QuotePrefix { get; set; } = string.Empty;

    public int DefaultQuoteValidityDays { get; set; }

    public string InvoicePrefix { get; set; } = string.Empty;

    public string ReceiptPrefix { get; set; } = string.Empty;
}
