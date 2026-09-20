namespace QuoteSnap.Application.Email;

public interface ISystemEmailService
{
    Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}