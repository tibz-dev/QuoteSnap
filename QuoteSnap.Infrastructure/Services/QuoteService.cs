using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Quotes;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class QuoteService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public QuoteService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<QuoteDto>> GetAllAsync()
    {
        var businessId = GetBusinessId();

        return await _dbContext.Quotes
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new QuoteDto
            {
                Id = x.Id,
                QuoteNumber = x.QuoteNumber,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                Status = x.Status,
                IssueDate = x.IssueDate,
                ValidUntil = x.ValidUntil,
                CurrencyCode = x.CurrencyCode,
                TaxName = x.TaxName,
                TaxRate = x.TaxRate,
                Subtotal = x.Subtotal,
                DiscountAmount = x.DiscountAmount,
                TaxAmount = x.TaxAmount,
                Total = x.Total,
                Notes = x.Notes,
                Terms = x.Terms,
                CreatedAt = x.CreatedAt,
                Items = x.Items
                    .Select(item => new QuoteItemDto
                    {
                        Id = item.Id,
                        CatalogueItemId = item.CatalogueItemId,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        LineSubtotal = item.LineSubtotal,
                        LineTotal = item.LineTotal
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<QuoteDto?> GetByIdAsync(Guid id)
    {
        var businessId = GetBusinessId();

        return await _dbContext.Quotes
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BusinessId == businessId)
            .Select(x => new QuoteDto
            {
                Id = x.Id,
                QuoteNumber = x.QuoteNumber,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                Status = x.Status,
                IssueDate = x.IssueDate,
                ValidUntil = x.ValidUntil,
                CurrencyCode = x.CurrencyCode,
                TaxName = x.TaxName,
                TaxRate = x.TaxRate,
                Subtotal = x.Subtotal,
                DiscountAmount = x.DiscountAmount,
                TaxAmount = x.TaxAmount,
                Total = x.Total,
                Notes = x.Notes,
                Terms = x.Terms,
                CreatedAt = x.CreatedAt,
                Items = x.Items
                    .Select(item => new QuoteItemDto
                    {
                        Id = item.Id,
                        CatalogueItemId = item.CatalogueItemId,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        LineSubtotal = item.LineSubtotal,
                        LineTotal = item.LineTotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<QuoteDto> CreateAsync(
        CreateQuoteRequest request)
    {
        var businessId = GetBusinessId();

        if (request.CustomerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer is required.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException(
                "A quote must contain at least one item.");
        }

        var business = await _dbContext.Businesses
            .FirstOrDefaultAsync(x => x.Id == businessId);

        if (business is null)
        {
            throw new InvalidOperationException(
                "Business could not be found.");
        }

        var customerExists = await _dbContext.Customers
            .AnyAsync(x =>
                x.Id == request.CustomerId &&
                x.BusinessId == businessId);

        if (!customerExists)
        {
            throw new ArgumentException(
                "The selected customer does not exist.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var quoteNumber =
                $"{business.QuotePrefix}-{business.NextQuoteNumber:D6}";

            business.NextQuoteNumber++;

            var quote = new Quote
            {
                BusinessId = businessId,
                CustomerId = request.CustomerId,
                QuoteNumber = quoteNumber,
                Status = QuoteStatus.Draft,
                IssueDate = DateTime.UtcNow,

                ValidUntil = request.ValidUntil
                    ?? DateTime.UtcNow.AddDays(
                        business.DefaultQuoteValidityDays),

                CurrencyCode =
                    GetCurrencyCode(
                        request.CurrencyCode,
                        business.CurrencyCode),

                TaxName = business.IsTaxRegistered
                    ? business.TaxName
                    : null,

                TaxRate = GetTaxRate(
                    request.TaxRate,
                    business),

                Notes = Clean(request.Notes),
                Terms = Clean(request.Terms)
            };

            foreach (var requestItem in request.Items)
            {
                var quoteItem = await BuildQuoteItemAsync(
                    businessId,
                    requestItem);

                quote.Items.Add(quoteItem);
            }

            quote.CalculateTotals();

            _dbContext.Quotes.Add(quote);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return await GetByIdAsync(quote.Id)
                ?? throw new InvalidOperationException(
                    "Quote could not be loaded after creation.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<QuoteItem> BuildQuoteItemAsync(
        Guid businessId,
        CreateQuoteItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException(
                "Item quantity must be greater than zero.");
        }

        if (request.DiscountAmount < 0)
        {
            throw new ArgumentException(
                "Item discount cannot be negative.");
        }

        string description;
        decimal unitPrice;

        if (request.CatalogueItemId.HasValue)
        {
            var catalogueItem =
                await _dbContext.CatalogueItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.CatalogueItemId.Value &&
                        x.BusinessId == businessId &&
                        x.IsActive);

            if (catalogueItem is null)
            {
                throw new ArgumentException(
                    "One of the selected catalogue items does not exist.");
            }

            description =
                !string.IsNullOrWhiteSpace(request.Description)
                    ? request.Description.Trim()
                    : catalogueItem.Name;

            if (request.UnitPrice.HasValue)
            {
                unitPrice = request.UnitPrice.Value;
            }
            else if (catalogueItem.DefaultPrice.HasValue)
            {
                unitPrice = catalogueItem.DefaultPrice.Value;
            }
            else
            {
                throw new ArgumentException(
                    $"A price is required for '{catalogueItem.Name}'.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ArgumentException(
                    "Description is required for a custom quote item.");
            }

            if (!request.UnitPrice.HasValue)
            {
                throw new ArgumentException(
                    "Unit price is required for a custom quote item.");
            }

            description = request.Description.Trim();
            unitPrice = request.UnitPrice.Value;
        }

        if (unitPrice < 0)
        {
            throw new ArgumentException(
                "Unit price cannot be negative.");
        }

        var lineSubtotal =
            request.Quantity * unitPrice;

        if (request.DiscountAmount > lineSubtotal)
        {
            throw new ArgumentException(
                $"Discount cannot exceed the line subtotal for '{description}'.");
        }

        var quoteItem = new QuoteItem
        {
            CatalogueItemId = request.CatalogueItemId,
            Description = description,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            DiscountAmount = request.DiscountAmount
        };

        quoteItem.CalculateTotal();

        return quoteItem;
    }

    private static string GetCurrencyCode(
        string? requestedCurrencyCode,
        string businessCurrencyCode)
    {
        var currencyCode =
            string.IsNullOrWhiteSpace(requestedCurrencyCode)
                ? businessCurrencyCode
                : requestedCurrencyCode.Trim().ToUpperInvariant();

        if (currencyCode.Length != 3)
        {
            throw new ArgumentException(
                "Currency code must contain exactly 3 characters.");
        }

        return currencyCode;
    }

    private static decimal GetTaxRate(
        decimal? requestedTaxRate,
        Business business)
    {
        if (!business.IsTaxRegistered)
        {
            return 0m;
        }

        var taxRate =
            requestedTaxRate
            ?? business.DefaultTaxRate
            ?? 0m;

        if (taxRate < 0 || taxRate > 100)
        {
            throw new ArgumentException(
                "Tax rate must be between 0 and 100.");
        }

        return taxRate;
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
}