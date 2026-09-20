namespace QuoteSnap.Infrastructure.Email;

public class GmailSettings
{
    public const string SectionName = "Gmail";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "QuoteSnap";
}