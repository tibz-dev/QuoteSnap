using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

            var providerMessageId =
                await GmailMessageSender.SendAsync(
                    _settings,
                    refreshToken,
                    message,
                    connection.EmailAddress,
                    business.Name,
                    message.ReplyTo,
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
                ProviderMessageId = providerMessageId
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
