using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class Business : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? LogoUrl { get; set; }

    // Location
    public string CountryCode { get; set; } = string.Empty;

    // Currency
    public string CurrencyCode { get; set; } = string.Empty;

    // Tax
    public bool IsTaxRegistered { get; set; }

    public string? TaxName { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public decimal? DefaultTaxRate { get; set; }

    // Banking
    public string? BankName { get; set; }

    public string? AccountHolder { get; set; }

    public string? AccountNumber { get; set; }

    public string? BranchCode { get; set; }

    // Document settings
    public string QuotePrefix { get; set; } = "QT";

    public int NextQuoteNumber { get; set; } = 1;

    public int DefaultQuoteValidityDays { get; set; } = 7;

    public string InvoicePrefix { get; set; } = "INV";

    public int NextInvoiceNumber { get; set; } = 1;


    public string ReceiptPrefix { get; set; } = "RCT";
    public int NextReceiptNumber { get; set; } = 1;

    // Subscription
    public SubscriptionPlan SubscriptionPlan { get; set; }
        = SubscriptionPlan.Free;

    public Subscription? Subscription { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Customer> Customers { get; set; }
        = new List<Customer>();

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public ICollection<CatalogueItem> CatalogueItems { get; set; }
        = new List<CatalogueItem>();

    public ICollection<Quote> Quotes { get; set; }
        = new List<Quote>();

    public ICollection<Invoice> Invoices { get; set; }
    = new List<Invoice>();

    public ICollection<Receipt> Receipts { get; set; }
    = new List<Receipt>();

    public ICollection<EmailConnection> EmailConnections { get; set; }
    = new List<EmailConnection>();

    public ICollection<DocumentDelivery> DocumentDeliveries { get; set; }
    = new List<DocumentDelivery>();

    public ICollection<Notification> Notifications { get; set; }
    = new List<Notification>();
}