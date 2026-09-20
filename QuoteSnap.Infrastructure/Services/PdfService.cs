using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Infrastructure.Pdf;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class PdfService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public PdfService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<(byte[] Content, string FileName)?>
        GenerateInvoiceAsync(Guid invoiceId)
    {
        var businessId = GetBusinessId();

        var business = await _dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == businessId);

        if (business is null)
        {
            throw new InvalidOperationException(
                "Business could not be found.");
        }

        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == invoiceId &&
                x.BusinessId == businessId);

        if (invoice is null)
        {
            return null;
        }

        var document =
            new InvoicePdfDocument(
                invoice,
                business);

        var pdf =
            document.GeneratePdf();

        var fileName =
            $"{invoice.InvoiceNumber}.pdf";

        return (pdf, fileName);
    }

    public async Task<(byte[] Content, string FileName)?>GenerateQuoteAsync(Guid quoteId)
    {
        var businessId = GetBusinessId();

        var business = await _dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == businessId);

        if (business is null)
        {
            throw new InvalidOperationException(
                "Business could not be found.");
        }

        var quote = await _dbContext.Quotes
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == quoteId &&
                x.BusinessId == businessId);

        if (quote is null)
        {
            return null;
        }

        var document =
            new QuotePdfDocument(
                quote,
                business);

        var pdf =
            document.GeneratePdf();

        return (
            pdf,
            $"{quote.QuoteNumber}.pdf");
    }

    public async Task<(byte[] Content, string FileName)?>GenerateReceiptAsync(Guid receiptId)
    {
        var businessId = GetBusinessId();

        var business = await _dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == businessId);

        if (business is null)
        {
            throw new InvalidOperationException(
                "Business could not be found.");
        }

        var receipt = await _dbContext.Receipts
            .AsNoTracking()
            .Include(x => x.Payment)
            .Include(x => x.Invoice)
                .ThenInclude(x => x.Customer)
            .FirstOrDefaultAsync(x =>
                x.Id == receiptId &&
                x.BusinessId == businessId);

        if (receipt is null)
        {
            return null;
        }

        var document =
            new ReceiptPdfDocument(
                receipt,
                business);

        var pdf =
            document.GeneratePdf();

        return (
            pdf,
            $"{receipt.ReceiptNumber}.pdf");
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