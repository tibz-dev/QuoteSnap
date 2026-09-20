using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Receipts;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class ReceiptService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ReceiptService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<ReceiptDto>> GetAllAsync()
    {
        var businessId = GetBusinessId();

        var receipts = await _dbContext.Receipts
            .AsNoTracking()
            .Include(x => x.Payment)
            .Include(x => x.Invoice)
                .ThenInclude(x => x.Customer)
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.ReceiptDate)
            .ToListAsync();

        return receipts
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ReceiptDto?> GetByIdAsync(Guid id)
    {
        var businessId = GetBusinessId();

        var receipt = await _dbContext.Receipts
            .AsNoTracking()
            .Include(x => x.Payment)
            .Include(x => x.Invoice)
                .ThenInclude(x => x.Customer)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        return receipt is null
            ? null
            : MapToDto(receipt);
    }

    public async Task<ReceiptDto> GenerateFromPaymentAsync(
        Guid paymentId)
    {
        var businessId = GetBusinessId();

        var business = await _dbContext.Businesses
            .FirstOrDefaultAsync(x =>
                x.Id == businessId);

        if (business is null)
        {
            throw new InvalidOperationException(
                "Business could not be found.");
        }

        var payment = await _dbContext.Payments
            .Include(x => x.Invoice)
                .ThenInclude(x => x.Customer)
            .FirstOrDefaultAsync(x =>
                x.Id == paymentId &&
                x.BusinessId == businessId);

        if (payment is null)
        {
            throw new ArgumentException(
                "Payment could not be found.");
        }

        var existingReceipt =
            await _dbContext.Receipts
                .AnyAsync(x =>
                    x.PaymentId == paymentId &&
                    x.BusinessId == businessId);

        if (existingReceipt)
        {
            throw new InvalidOperationException(
                "A receipt has already been generated for this payment.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var receiptNumber =
                $"{business.ReceiptPrefix}-{business.NextReceiptNumber:D6}";

            business.NextReceiptNumber++;

            var receipt = new Receipt
            {
                BusinessId = businessId,

                ReceiptNumber = receiptNumber,

                PaymentId = payment.Id,

                InvoiceId = payment.InvoiceId,

                ReceiptDate = DateTime.UtcNow,

                CurrencyCode =
                    payment.Invoice.CurrencyCode,

                Amount =
                    payment.Amount
            };

            _dbContext.Receipts.Add(receipt);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return await GetByIdAsync(receipt.Id)
                ?? throw new InvalidOperationException(
                    "Receipt could not be loaded after creation.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static ReceiptDto MapToDto(
        Receipt receipt)
    {
        return new ReceiptDto
        {
            Id = receipt.Id,

            ReceiptNumber =
                receipt.ReceiptNumber,

            PaymentId =
                receipt.PaymentId,

            InvoiceId =
                receipt.InvoiceId,

            InvoiceNumber =
                receipt.Invoice?.InvoiceNumber
                ?? string.Empty,

            CustomerId =
                receipt.Invoice?.CustomerId
                ?? Guid.Empty,

            CustomerName =
                receipt.Invoice?.Customer?.Name
                ?? "Unknown Customer",

            ReceiptDate =
                receipt.ReceiptDate,

            CurrencyCode =
                receipt.CurrencyCode,

            Amount =
                receipt.Amount,

            PaymentMethod =
                receipt.Payment?.Method
                ?? default,

            PaymentDate =
                receipt.Payment?.PaymentDate
                ?? receipt.ReceiptDate,

            PaymentReference =
                receipt.Payment?.Reference,

            PaymentNotes =
                receipt.Payment?.Notes,

            CreatedAt =
                receipt.CreatedAt
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