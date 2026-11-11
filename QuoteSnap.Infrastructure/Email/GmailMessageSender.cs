using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;
using QuoteSnap.Application.Email;

namespace QuoteSnap.Infrastructure.Email;

internal static class GmailMessageSender
{
    public static async Task<string?> SendAsync(
        GmailSettings settings,
        string refreshToken,
        EmailMessage message,
        string senderEmail,
        string senderName,
        string? replyTo,
        CancellationToken cancellationToken)
    {
        using var flow =
            new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets =
                        new ClientSecrets
                        {
                            ClientId =
                                settings.ClientId,

                            ClientSecret =
                                settings.ClientSecret
                        },

                    Scopes =
                    [
                        GmailService.Scope.GmailSend
                    ]
                });

        var tokenResponse =
            new TokenResponse
            {
                RefreshToken = refreshToken
            };

        var credential =
            new UserCredential(
                flow,
                senderEmail,
                tokenResponse);

        using var gmailService =
            new GmailService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer =
                        credential,

                    ApplicationName =
                        "QuoteSnap"
                });

        var mimeMessage =
            BuildMimeMessage(
                message,
                senderEmail,
                senderName,
                replyTo);

        using var stream =
            new MemoryStream();

        await mimeMessage.WriteToAsync(
            stream,
            cancellationToken);

        var rawMessage =
            Convert.ToBase64String(
                    stream.ToArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

        var gmailMessage =
            new Message
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

        return response.Id;
    }

    private static MimeMessage BuildMimeMessage(
        EmailMessage message,
        string senderEmail,
        string senderName,
        string? replyTo)
    {
        var mimeMessage =
            new MimeMessage();

        mimeMessage.From.Add(
            new MailboxAddress(
                senderName,
                senderEmail));

        mimeMessage.To.Add(
            MailboxAddress.Parse(
                message.To));

        mimeMessage.Subject =
            message.Subject;

        if (!string.IsNullOrWhiteSpace(
                replyTo))
        {
            mimeMessage.ReplyTo.Add(
                MailboxAddress.Parse(
                    replyTo));
        }

        var bodyBuilder =
            new BodyBuilder
            {
                HtmlBody = message.HtmlBody
            };

        foreach (var attachment
                 in message.Attachments)
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
}
