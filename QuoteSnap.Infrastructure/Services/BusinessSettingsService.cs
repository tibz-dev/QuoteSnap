using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Businesses;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class BusinessSettingsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public BusinessSettingsService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<BusinessSettingsDto> GetCurrentAsync()
    {
        var businessId = GetBusinessId();

        var business = await _dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId)
            ?? throw new InvalidOperationException(
                "Business could not be found.");

        return Map(business);
    }

    public async Task<BusinessSettingsDto> UpdateAsync(
        UpdateBusinessSettingsRequest request)
    {
        var businessId = GetBusinessId();

        Validate(request);

        var business = await _dbContext.Businesses
            .FirstOrDefaultAsync(x => x.Id == businessId)
            ?? throw new InvalidOperationException(
                "Business could not be found.");

        business.Name = request.Name.Trim();
        business.Email = Clean(request.Email);
        business.Phone = Clean(request.Phone);
        business.Address = Clean(request.Address);
        business.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        business.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();

        business.IsTaxRegistered = request.IsTaxRegistered;
        business.TaxName = request.IsTaxRegistered
            ? Clean(request.TaxName)
            : null;
        business.TaxRegistrationNumber = request.IsTaxRegistered
            ? Clean(request.TaxRegistrationNumber)
            : null;
        business.DefaultTaxRate = request.IsTaxRegistered
            ? request.DefaultTaxRate
            : null;

        business.BankName = Clean(request.BankName);
        business.AccountHolder = Clean(request.AccountHolder);
        business.AccountNumber = Clean(request.AccountNumber);
        business.BranchCode = Clean(request.BranchCode);

        business.QuotePrefix = NormalizePrefix(request.QuotePrefix);
        business.DefaultQuoteValidityDays =
            request.DefaultQuoteValidityDays;
        business.InvoicePrefix = NormalizePrefix(request.InvoicePrefix);
        business.ReceiptPrefix = NormalizePrefix(request.ReceiptPrefix);
        business.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Map(business);
    }

    private static void Validate(
        UpdateBusinessSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Business name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CountryCode) ||
            request.CountryCode.Trim().Length != 2)
        {
            throw new ArgumentException(
                "Country code must contain exactly 2 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrencyCode) ||
            request.CurrencyCode.Trim().Length != 3)
        {
            throw new ArgumentException(
                "Currency code must contain exactly 3 characters.");
        }

        if (request.IsTaxRegistered &&
            request.DefaultTaxRate.HasValue &&
            (request.DefaultTaxRate.Value < 0 ||
             request.DefaultTaxRate.Value > 100))
        {
            throw new ArgumentException(
                "Default tax rate must be between 0 and 100.");
        }

        if (request.DefaultQuoteValidityDays < 1 ||
            request.DefaultQuoteValidityDays > 365)
        {
            throw new ArgumentException(
                "Quote validity must be between 1 and 365 days.");
        }

        ValidatePrefix(request.QuotePrefix, "Quote");
        ValidatePrefix(request.InvoicePrefix, "Invoice");
        ValidatePrefix(request.ReceiptPrefix, "Receipt");
    }

    private static void ValidatePrefix(
        string prefix,
        string label)
    {
        var normalized = NormalizePrefix(prefix);

        if (normalized.Length > 10)
        {
            throw new ArgumentException(
                $"{label} prefix cannot exceed 10 characters.");
        }
    }

    private static string NormalizePrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Document prefixes are required.");
        }

        return value.Trim().ToUpperInvariant();
    }

    private Guid GetBusinessId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.BusinessId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Business information is missing.");
        }

        return _currentUser.BusinessId;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static BusinessSettingsDto Map(
        QuoteSnap.Domain.Entities.Business business)
    {
        return new BusinessSettingsDto
        {
            Id = business.Id,
            Name = business.Name,
            Email = business.Email,
            Phone = business.Phone,
            Address = business.Address,
            CountryCode = business.CountryCode,
            CurrencyCode = business.CurrencyCode,
            IsTaxRegistered = business.IsTaxRegistered,
            TaxName = business.TaxName,
            TaxRegistrationNumber =
                business.TaxRegistrationNumber,
            DefaultTaxRate = business.DefaultTaxRate,
            BankName = business.BankName,
            AccountHolder = business.AccountHolder,
            AccountNumber = business.AccountNumber,
            BranchCode = business.BranchCode,
            QuotePrefix = business.QuotePrefix,
            DefaultQuoteValidityDays =
                business.DefaultQuoteValidityDays,
            InvoicePrefix = business.InvoicePrefix,
            ReceiptPrefix = business.ReceiptPrefix
        };
    }
}
