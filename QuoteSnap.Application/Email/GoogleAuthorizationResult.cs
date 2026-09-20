namespace QuoteSnap.Application.Email;

public class GoogleAuthorizationResult
{
    public string AccessToken { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}