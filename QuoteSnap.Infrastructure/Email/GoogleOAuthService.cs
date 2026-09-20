using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Options;
using QuoteSnap.Application.Email;

namespace QuoteSnap.Infrastructure.Email;

public class GoogleOAuthService
{
    private readonly GmailSettings _settings;

    public GoogleOAuthService(
        IOptions<GmailSettings> options)
    {
        _settings = options.Value;
    }

    public string GetAuthorizationUrl(
        string redirectUri)
    {
        var flow = CreateFlow();

        var request =
            flow.CreateAuthorizationCodeRequest(
                redirectUri);

        var authorizationUrl =
            request.Build()
                .ToString();

        // Request offline access so Google can return
        // a refresh token.
        authorizationUrl +=
            "&access_type=offline&prompt=consent";

        return authorizationUrl;
    }

    public async Task<GoogleAuthorizationResult>
        ExchangeCodeAsync(
            string code,
            string redirectUri,
            CancellationToken cancellationToken = default)
    {
        var flow = CreateFlow();

        TokenResponse token =
            await flow.ExchangeCodeForTokenAsync(
                "quotesnap-sender",
                code,
                redirectUri,
                cancellationToken);

        return new GoogleAuthorizationResult
        {
            AccessToken =
                token.AccessToken ?? string.Empty,

            RefreshToken =
                token.RefreshToken,

            ExpiresAtUtc =
                token.ExpiresInSeconds.HasValue
                    ? DateTime.UtcNow.AddSeconds(
                        token.ExpiresInSeconds.Value)
                    : null
        };
    }

    private GoogleAuthorizationCodeFlow CreateFlow()
    {
        return new GoogleAuthorizationCodeFlow(
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
                    "https://www.googleapis.com/auth/gmail.send"
                ]
            });
    }
}