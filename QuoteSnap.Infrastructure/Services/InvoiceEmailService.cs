using System.Net;
using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Email;
using QuoteSnap.Application.Invoices;
using QuoteSnap.Infrastructure.Persistence;

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

        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == invoiceId &&
                    x.BusinessId == businessId,
                cancellationToken);

        if (invoice is null)
            throw new ArgumentException("Invoice not found.");

        var business = await _dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == businessId,
                cancellationToken);

        if (business is null)
            throw new InvalidOperationException(
                "Business could not be found.");

        var recipient =
            string.IsNullOrWhiteSpace(request.To)
                ? invoice.Customer.Email
                : request.To.Trim();

        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException(
                "The customer does not have an email address.");
        }

        var pdf =
            await _pdfService.GenerateInvoiceAsync(invoiceId);

        if (pdf is null)
            throw new InvalidOperationException(
                "Invoice PDF could not be generated.");

        var subject =
            string.IsNullOrWhiteSpace(request.Subject)
                ? $"Invoice {invoice.InvoiceNumber} from {business.Name}"
                : request.Subject.Trim();

        var customMessage =
            string.IsNullOrWhiteSpace(request.Message)
                ? "Please find your invoice attached."
                : request.Message.Trim();

        var htmlBody = BuildHtmlBody(
            business.Name,
            invoice.Customer.Name,
            invoice.InvoiceNumber,
            invoice.CurrencyCode,
            invoice.Total,
            invoice.DueDate,
            customMessage);

        var message = new EmailMessage
        {
            To = recipient,
            Subject = subject,
            HtmlBody = htmlBody,

            ReplyTo = string.IsNullOrWhiteSpace(business.Email)
                ? null
                : business.Email,

            Attachments =
            {
                new EmailAttachment
                {
                    FileName = pdf.Value.FileName,
                    ContentType = "application/pdf",
                    Content = pdf.Value.Content
                }
            }
        };

        return await _emailService.SendAsync(
            message,
            cancellationToken);
    }

    private static string BuildHtmlBody(
        string businessName,
        string customerName,
        string invoiceNumber,
        string currencyCode,
        decimal total,
        DateTime dueDate,
        string customMessage)
    {
        return $"""
            <div style="font-family:Arial,sans-serif;
                        max-width:600px;
                        margin:auto;
                        color:#222;">

                <h2>{WebUtility.HtmlEncode(businessName)}</h2>

                <p>
                    Hi {WebUtility.HtmlEncode(customerName)},
                </p>

                <p>
                    {WebUtility.HtmlEncode(customMessage)}
                </p>

                <p>
                    <strong>Invoice:</strong>
                    {WebUtility.HtmlEncode(invoiceNumber)}
                    <br />

                    <strong>Amount:</strong>
                    {WebUtility.HtmlEncode(currencyCode)}
                    {total:N2}
                    <br />

                    <strong>Due date:</strong>
                    {dueDate:dd MMM yyyy}
                </p>

                <p>
                    The invoice PDF is attached to this email.
                </p>

                <p>
                    Regards,<br />
                    {WebUtility.HtmlEncode(businessName)}
                </p>

                <hr />

                <small>
                    Sent using QuoteSnap
                </small>

            </div>
            """;
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