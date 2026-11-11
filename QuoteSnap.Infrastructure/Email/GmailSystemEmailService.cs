using Microsoft.Extensions.Options;
using QuoteSnap.Application.Email;

namespace QuoteSnap.Infrastructure.Email;

public class GmailSystemEmailService(
    IOptions<GmailSettings> gmailOptions,
    IOptions<SystemEmailSettings> systemEmailOptions) : ISystemEmailService
{
    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var gmail = gmailOptions.Value;
        var systemEmail = systemEmailOptions.Value;

        if (string.IsNullOrWhiteSpace(gmail.ClientId) ||
            string.IsNullOrWhiteSpace(gmail.ClientSecret) ||
            string.IsNullOrWhiteSpace(systemEmail.RefreshToken) ||
            string.IsNullOrWhiteSpace(systemEmail.SenderEmail))
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage =
                    "System email requires Gmail OAuth client credentials, a sender refresh token, and a sender email."
            };
        }

        var providerMessageId = await GmailMessageSender.SendAsync(
            gmail,
            systemEmail.RefreshToken,
            message,
            systemEmail.SenderEmail,
            systemEmail.SenderName,
            systemEmail.ReplyToEmail,
            cancellationToken);

        return new EmailSendResult
        {
            Success = true,
            ProviderMessageId = providerMessageId
        };
    }
}
