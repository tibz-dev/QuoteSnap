using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Email;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class DocumentDeliveryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public DocumentDeliveryService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<DocumentDeliveryDto>>
        GetInvoiceDeliveriesAsync(
            Guid invoiceId,
            CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();

        var invoiceExists =
            await _dbContext.Invoices
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == invoiceId &&
                        x.BusinessId == businessId,
                    cancellationToken);

        if (!invoiceExists)
        {
            throw new InvalidOperationException(
                "Invoice could not be found.");
        }

        return await _dbContext.DocumentDeliveries
            .AsNoTracking()
            .Where(x =>
                x.BusinessId == businessId &&
                x.DocumentType == DocumentType.Invoice &&
                x.DocumentId == invoiceId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DocumentDeliveryDto
            {
                Id = x.Id,
                DocumentType = x.DocumentType,
                DocumentId = x.DocumentId,
                DocumentNumber = x.DocumentNumber,
                Channel = x.Channel,
                Recipient = x.Recipient,
                Status = x.Status,
                Provider = x.Provider,
                ProviderMessageId = x.ProviderMessageId,
                ErrorMessage = x.ErrorMessage,
                SentAt = x.SentAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
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