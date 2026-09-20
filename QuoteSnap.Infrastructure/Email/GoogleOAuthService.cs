using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuoteSnap.Application.Security;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Email;

public class GoogleOAuthService
{
    private const string GmailSendScope =
        "https://www.googleapis.com/auth/gmail.send";

    private const string UserEmailScope =
        "https://www.googleapis.com/auth/userinfo.email";

    private readonly GmailSettings _settings;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITokenProtectionService _tokenProtection;

    public GoogleOAuthService(
        IOptions<GmailSettings> options,
        ApplicationDbContext dbContext,
        ITokenProtectionService tokenProtection)
    {
        _settings = options.Value;
        _dbContext = dbContext;
        _tokenProtection = tokenProtection;
    }

    public string GetAuthorizationUrl(string state)
    {
        ValidateSettings();

        var flow = CreateFlow();

        var request =
            flow.CreateAuthorizationCodeRequest(
                _settings.RedirectUri);

        var authorizationUrl =
            request.Build().ToString();

        authorizationUrl +=
            $"&prompt=consent&state={Uri.EscapeDataString(state)}";

        return authorizationUrl;
    }

    public async Task ConnectAsync(
        string code,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Google authorization code is missing.");
        }

        var flow = CreateFlow();

        TokenResponse token =
            await flow.ExchangeCodeForTokenAsync(
                businessId.ToString(),
                code,
                _settings.RedirectUri,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            throw new InvalidOperationException(
                "Google did not return a refresh token. " +
                "Remove QuoteSnap from your Google account permissions and connect again.");
        }

        var credential =
            new UserCredential(
                flow,
                businessId.ToString(),
                token);

        using var oauthService =
            new Oauth2Service(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "QuoteSnap"
                });

        var userInfo =
            await oauthService.Userinfo
                .Get()
                .ExecuteAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userInfo.Email))
        {
            throw new InvalidOperationException(
                "Google account email could not be determined.");
        }

        var encryptedRefreshToken =
            _tokenProtection.Protect(
                token.RefreshToken);

        var connection =
            await _dbContext.EmailConnections
                .FirstOrDefaultAsync(
                    x =>
                        x.BusinessId == businessId &&
                        x.Provider == EmailProvider.Google,
                    cancellationToken);

        if (connection is null)
        {
            connection = new EmailConnection
            {
                BusinessId = businessId,
                Provider = EmailProvider.Google,
                EmailAddress = userInfo.Email,
                EncryptedRefreshToken =
                    encryptedRefreshToken,
                IsActive = true,
                ConnectedAt = DateTime.UtcNow
            };

            _dbContext.EmailConnections.Add(connection);
        }
        else
        {
            connection.EmailAddress =
                userInfo.Email;

            connection.EncryptedRefreshToken =
                encryptedRefreshToken;

            connection.IsActive = true;

            connection.ConnectedAt =
                DateTime.UtcNow;

            connection.UpdatedAt =
                DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private GoogleAuthorizationCodeFlow CreateFlow()
    {
        return new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets =
                    new ClientSecrets
                    {
                        ClientId = _settings.ClientId,
                        ClientSecret = _settings.ClientSecret
                    },

                Scopes =
                [
                    GmailSendScope,
                    UserEmailScope
                ]
            });
    }

    private void ValidateSettings()
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

        if (string.IsNullOrWhiteSpace(
                _settings.RedirectUri))
        {
            throw new InvalidOperationException(
                "Google RedirectUri is missing.");
        }
    }
}