using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Invoices;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class InvoiceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public InvoiceService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<InvoiceDto>> GetAllAsync()
    {
        var businessId = GetBusinessId();

        var invoices = await _dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return invoices
            .Select(MapProjection)
            .ToList();
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var businessId = GetBusinessId();

        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (invoice is null)
        {
            return null;
        }

        return MapProjection(invoice);
    }

    public async Task<InvoiceDto> ConvertQuoteAsync(
        Guid quoteId,
        ConvertQuoteToInvoiceRequest request)
    {
        var businessId = GetBusinessId();

        var business = await _dbContext.Businesses
            .FirstOrDefaultAsync(x => x.Id == businessId);

        if (business is null)
        {
            throw new InvalidOperationException(
                "Business could not be found.");
        }

        var quote = await _dbContext.Quotes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == quoteId &&
                x.BusinessId == businessId);

        if (quote is null)
        {
            throw new ArgumentException(
                "Quote could not be found.");
        }

        if (quote.Status != QuoteStatus.Accepted)
        {
            throw new InvalidOperationException(
                "Only an accepted quote can be converted to an invoice.");
        }

        var alreadyConverted = await _dbContext.Invoices
            .AnyAsync(x =>
                x.BusinessId == businessId &&
                x.QuoteId == quoteId);

        if (alreadyConverted)
        {
            throw new InvalidOperationException(
                "This quote has already been converted to an invoice.");
        }

        var issueDate = DateTime.UtcNow;

        var dueDate =
            request.DueDate ?? issueDate.AddDays(30);

        if (dueDate < issueDate)
        {
            throw new ArgumentException(
                "Invoice due date cannot be before the issue date.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var invoiceNumber =
                $"{business.InvoicePrefix}-{business.NextInvoiceNumber:D6}";

            business.NextInvoiceNumber++;

            var invoice = new Invoice
            {
                BusinessId = businessId,
                CustomerId = quote.CustomerId,
                QuoteId = quote.Id,

                InvoiceNumber = invoiceNumber,

                Status = InvoiceStatus.Draft,

                IssueDate = issueDate,
                DueDate = dueDate,

                CurrencyCode = quote.CurrencyCode,

                TaxName = quote.TaxName,
                TaxRate = quote.TaxRate,

                Subtotal = quote.Subtotal,
                DiscountAmount = quote.DiscountAmount,
                TaxAmount = quote.TaxAmount,
                Total = quote.Total,

                AmountPaid = 0m,

                Notes = quote.Notes,
                Terms = quote.Terms
            };

            foreach (var quoteItem in quote.Items)
            {
                invoice.Items.Add(
                    new InvoiceItem
                    {
                        CatalogueItemId =
                            quoteItem.CatalogueItemId,

                        Description =
                            quoteItem.Description,

                        Quantity =
                            quoteItem.Quantity,

                        UnitPrice =
                            quoteItem.UnitPrice,

                        DiscountAmount =
                            quoteItem.DiscountAmount,

                        LineSubtotal =
                            quoteItem.LineSubtotal,

                        LineTotal =
                            quoteItem.LineTotal
                    });
            }

            quote.Status =
                QuoteStatus.ConvertedToInvoice;

            quote.UpdatedAt =
                DateTime.UtcNow;

            _dbContext.Invoices.Add(invoice);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return await GetByIdAsync(invoice.Id)
                ?? throw new InvalidOperationException(
                    "Invoice could not be loaded after creation.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<InvoiceDto?> RecordPaymentAsync(
    Guid id,
    RecordPaymentRequest request)
    {
        var businessId = GetBusinessId();

        if (request.Amount <= 0)
        {
            throw new ArgumentException(
                "Payment amount must be greater than zero.");
        }

        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Payments cannot be recorded against a cancelled invoice.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException(
                "This invoice has already been paid in full.");
        }

        var remainingBalance =
            invoice.Total - invoice.AmountPaid;

        if (request.Amount > remainingBalance)
        {
            throw new ArgumentException(
                $"Payment cannot exceed the remaining balance of {remainingBalance:0.00} {invoice.CurrencyCode}.");
        }

        invoice.AmountPaid += request.Amount;

        if (invoice.AmountPaid >= invoice.Total)
        {
            invoice.Status = InvoiceStatus.Paid;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        invoice.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    private static InvoiceDto MapProjection(Invoice invoice)
    {
        if (invoice is null)
        {
            throw new ArgumentNullException(nameof(invoice));
        }

        return new InvoiceDto
        {
            Id = invoice.Id,

            InvoiceNumber =
                invoice.InvoiceNumber ?? string.Empty,

            CustomerId =
                invoice.CustomerId,

            CustomerName =
                invoice.Customer?.Name ?? "Unknown Customer",

            QuoteId =
                invoice.QuoteId,

            Status =
                invoice.Status,

            IssueDate =
                invoice.IssueDate,

            DueDate =
                invoice.DueDate,

            CurrencyCode =
                invoice.CurrencyCode ?? string.Empty,

            TaxName =
                invoice.TaxName,

            TaxRate =
                invoice.TaxRate,

            Subtotal =
                invoice.Subtotal,

            DiscountAmount =
                invoice.DiscountAmount,

            TaxAmount =
                invoice.TaxAmount,

            Total =
                invoice.Total,

            AmountPaid =
                invoice.AmountPaid,

            BalanceDue =
                invoice.Total - invoice.AmountPaid,

            Notes =
                invoice.Notes,

            Terms =
                invoice.Terms,

            CreatedAt =
                invoice.CreatedAt,

            Items = invoice.Items?
                .Select(item => new InvoiceItemDto
                {
                    Id = item.Id,

                    CatalogueItemId =
                        item.CatalogueItemId,

                    Description =
                        item.Description ?? string.Empty,

                    Quantity =
                        item.Quantity,

                    UnitPrice =
                        item.UnitPrice,

                    DiscountAmount =
                        item.DiscountAmount,

                    LineSubtotal =
                        item.LineSubtotal,

                    LineTotal =
                        item.LineTotal
                })
                .ToList()
                ?? new List<InvoiceItemDto>()
        };
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
}