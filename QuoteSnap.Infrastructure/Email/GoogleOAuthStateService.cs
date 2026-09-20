using Microsoft.AspNetCore.DataProtection;

namespace QuoteSnap.Infrastructure.Email;

public class GoogleOAuthStateService
{
    private const string Purpose =
        "QuoteSnap.GoogleOAuth.State.v1";

    private readonly IDataProtector _protector;

    public GoogleOAuthStateService(
        IDataProtectionProvider provider)
    {
        _protector =
            provider.CreateProtector(Purpose);
    }

    public string Create(
        Guid businessId,
        Guid userId)
    {
        var expiresAt =
            DateTimeOffset.UtcNow
                .AddMinutes(10)
                .ToUnixTimeSeconds();

        var value =
            $"{businessId}|{userId}|{expiresAt}";

        return _protector.Protect(value);
    }

    public GoogleOAuthState Read(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException(
                "OAuth state is missing.");
        }

        string value;

        try
        {
            value = _protector.Unprotect(state);
        }
        catch
        {
            throw new InvalidOperationException(
                "OAuth state is invalid.");
        }

        var parts = value.Split('|');

        if (parts.Length != 3 ||
            !Guid.TryParse(parts[0], out var businessId) ||
            !Guid.TryParse(parts[1], out var userId) ||
            !long.TryParse(parts[2], out var expiresAt))
        {
            throw new InvalidOperationException(
                "OAuth state is invalid.");
        }

        var expiry =
            DateTimeOffset.FromUnixTimeSeconds(expiresAt);

        if (expiry < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException(
                "OAuth state has expired.");
        }

        return new GoogleOAuthState(
            businessId,
            userId);
    }
}

public record GoogleOAuthState(
    Guid BusinessId,
    Guid UserId);