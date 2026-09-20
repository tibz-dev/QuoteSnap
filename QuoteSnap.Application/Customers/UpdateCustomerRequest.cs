namespace QuoteSnap.Application.Customers;

public class UpdateCustomerRequest
{
    public string Name { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? TaxRegistrationNumber { get; set; }
}