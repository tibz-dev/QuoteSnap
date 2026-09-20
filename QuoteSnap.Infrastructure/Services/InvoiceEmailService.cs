using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Email;
using QuoteSnap.Application.Invoices;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Pdf;
using QuoteSnap.Infrastructure.Persistence;
using System.Net;

namespace QuoteSnap.Infrastructure.Services;

public class InvoiceEmailService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly PdfService _pdfService;

    public InvoiceEmailService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IEmailService emailService,
        PdfService pdfService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _emailService = emailService;
        _pdfService = pdfService;
    }

    public async Task<EmailSendResult> SendAsync(
        Guid invoiceId,
        SendInvoiceEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();

        var invoice =
            await _dbContext.Invoices
                .AsNoTracking()
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == invoiceId &&
                        x.BusinessId == businessId,
                    cancellationToken);

        if (invoice is null)
        {
            throw new InvalidOperationException(
                "Invoice could not be found.");
        }

        var business =
            await _dbContext.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == businessId,
                    cancellationToken);

        if (business is null)
        {
            throw new InvalidOperationException(
                "Business could not be found.");
        }

        var recipient =
            !string.IsNullOrWhiteSpace(request.To)
                ? request.To.Trim()
                : invoice.Customer?.Email?.Trim();

        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException(
                "A recipient email address is required.");
        }

        var delivery =
            new DocumentDelivery
            {
                BusinessId = businessId,
                DocumentType = DocumentType.Invoice,
                DocumentId = invoice.Id,
                DocumentNumber = invoice.InvoiceNumber,
                Channel = DeliveryChannel.Email,
                Recipient = recipient,
                Status = DeliveryStatus.Pending,
                Provider = "Google"
            };

        _dbContext.DocumentDeliveries.Add(delivery);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        try
        {
            var pdf =
                await _pdfService.GenerateInvoiceAsync(
                    invoiceId);

            if (pdf is null)
            {
                throw new InvalidOperationException(
                    "Invoice PDF could not be generated.");
            }

            var pdfResult = pdf.Value;

            var subject =
                !string.IsNullOrWhiteSpace(request.Subject)
                    ? request.Subject.Trim()
                    : $"Invoice {invoice.InvoiceNumber} from {business.Name}";

            var plainMessage =
                !string.IsNullOrWhiteSpace(request.Message)
                    ? request.Message.Trim()
                    : $"Please find invoice {invoice.InvoiceNumber} attached.";

            var email =
                new EmailMessage
                {
                    To = recipient,
                    Subject = subject,
                    ReplyTo = business.Email,
                    HtmlBody = BuildHtmlBody(
                        business.Name,
                        invoice.InvoiceNumber,
                        plainMessage),
                    Attachments =
                    [
                        new EmailAttachment
                        {
                            FileName = pdfResult.FileName,
                            ContentType = "application/pdf",
                            Content = pdfResult.Content
                        }
                    ]
                };

            var result =
                await _emailService.SendAsync(
                    email,
                    cancellationToken);

            if (result.Success)
            {
                delivery.Status =
                    DeliveryStatus.Sent;

                delivery.ProviderMessageId =
                    result.ProviderMessageId;

                delivery.SentAt =
                    DateTime.UtcNow;

                delivery.ErrorMessage =
                    null;
            }
            else
            {
                delivery.Status =
                    DeliveryStatus.Failed;

                delivery.ErrorMessage =
                    LimitErrorMessage(
                        result.ErrorMessage);
            }

            delivery.UpdatedAt =
                DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            delivery.Status =
                DeliveryStatus.Failed;

            delivery.ErrorMessage =
                LimitErrorMessage(ex.Message);

            delivery.UpdatedAt =
                DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                CancellationToken.None);

            throw;
        }
    }

    private static string BuildHtmlBody(
        string businessName,
        string invoiceNumber,
        string message)
    {
        var safeBusinessName =
            WebUtility.HtmlEncode(businessName);

        var safeInvoiceNumber =
            WebUtility.HtmlEncode(invoiceNumber);

        var safeMessage =
            WebUtility.HtmlEncode(message)
                .Replace("\r\n", "<br />")
                .Replace("\n", "<br />");

        return $"""
            <div style="font-family:Arial,sans-serif;line-height:1.6;">
                <p>{safeMessage}</p>

                <p>
                    Invoice:
                    <strong>{safeInvoiceNumber}</strong>
                </p>

                <p>
                    Regards,<br />
                    {safeBusinessName}
                </p>
            </div>
            """;
    }

    private static string? LimitErrorMessage(
        string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return null;

        return errorMessage.Length <= 2000
            ? errorMessage
            : errorMessage[..2000];
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