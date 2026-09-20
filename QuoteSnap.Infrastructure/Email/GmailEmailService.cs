using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using QuoteSnap.Application.Email;

namespace QuoteSnap.Infrastructure.Email;

public class GmailEmailService : IEmailService
{
    private readonly GmailSettings _settings;

    public GmailEmailService(
        IOptions<GmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateSettings();

            var credential = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = _settings.ClientId,
                            ClientSecret = _settings.ClientSecret
                        }
                    }),
                "quotesnap-sender",
                new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                {
                    RefreshToken = _settings.RefreshToken
                });

            using var gmailService = new GmailService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "QuoteSnap"
                });

            var mimeMessage = BuildMimeMessage(message);

            using var stream = new MemoryStream();

            await mimeMessage.WriteToAsync(
                stream,
                cancellationToken);

            var rawMessage =
                Convert.ToBase64String(stream.ToArray())
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

            var gmailMessage = new Message
            {
                Raw = rawMessage
            };

            var request =
                gmailService.Users.Messages.Send(
                    gmailMessage,
                    "me");

            var response =
                await request.ExecuteAsync(
                    cancellationToken);

            return new EmailSendResult
            {
                Success = true,
                ProviderMessageId = response.Id
            };
        }
        catch (Exception ex)
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private MimeMessage BuildMimeMessage(
        EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(
            new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

        mimeMessage.To.Add(
            MailboxAddress.Parse(message.To));

        mimeMessage.Subject =
            message.Subject;

        if (!string.IsNullOrWhiteSpace(
                message.ReplyTo))
        {
            mimeMessage.ReplyTo.Add(
                MailboxAddress.Parse(
                    message.ReplyTo));
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody
        };

        foreach (var attachment in message.Attachments)
        {
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(
                    attachment.ContentType));
        }

        mimeMessage.Body =
            bodyBuilder.ToMessageBody();

        return mimeMessage;
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(
                _settings.ClientId))
        {
            throw new InvalidOperationException(
                "ClientId is missing.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.ClientSecret))
        {
            throw new InvalidOperationException(
                "ClientSecret is missing.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.RefreshToken))
        {
            throw new InvalidOperationException(
                "RefreshToken is missing.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "SenderEmail is missing.");
        }
    }
}