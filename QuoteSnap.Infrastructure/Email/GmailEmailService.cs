using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Email;
using QuoteSnap.Application.Security;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Email;

public class GmailEmailService : IEmailService
{
    private readonly GmailSettings _settings;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenProtectionService _tokenProtection;

    public GmailEmailService(
        IOptions<GmailSettings> options,
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        ITokenProtectionService tokenProtection)
    {
        _settings = options.Value;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _tokenProtection = tokenProtection;
    }

    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePlatformSettings();

            var businessId = GetBusinessId();

            var business =
                await _dbContext.Businesses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == businessId,
                        cancellationToken);

            if (business is null)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage =
                        "Business could not be found."
                };
            }

            var connection =
                await _dbContext.EmailConnections
                    .FirstOrDefaultAsync(
                        x =>
                            x.BusinessId == businessId &&
                            x.Provider == EmailProvider.Google &&
                            x.IsActive,
                        cancellationToken);

            if (connection is null)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage =
                        "No active Google email account is connected to this business."
                };
            }

            var refreshToken =
                _tokenProtection.Unprotect(
                    connection.EncryptedRefreshToken);

            var flow =
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets =
                            new ClientSecrets
                            {
                                ClientId =
                                    _settings.ClientId,

                                ClientSecret =
                                    _settings.ClientSecret
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
                    businessId.ToString(),
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
                    connection.EmailAddress,
                    business.Name);

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

            connection.LastUsedAt =
                DateTime.UtcNow;

            connection.UpdatedAt =
                DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
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

    private static MimeMessage BuildMimeMessage(
        EmailMessage message,
        string senderEmail,
        string senderName)
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
                message.ReplyTo))
        {
            mimeMessage.ReplyTo.Add(
                MailboxAddress.Parse(
                    message.ReplyTo));
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

    private void ValidatePlatformSettings()
    {
        if (string.IsNullOrWhiteSpace(
                _settings.ClientId))
        {
            throw new InvalidOperationException(
                "Google ClientId is missing.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.ClientSecret))
        {
            throw new InvalidOperationException(
                "Google ClientSecret is missing.");
        }
    }
}