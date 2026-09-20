namespace QuoteSnap.Infrastructure.Email;

public class GmailSettings
{
    public const string SectionName = "Gmail";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;
}