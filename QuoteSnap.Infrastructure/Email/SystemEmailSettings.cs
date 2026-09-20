namespace QuoteSnap.Infrastructure.Email;

public class SystemEmailSettings
{
    public const string SectionName = "SystemEmail";

    public string ApiKey { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "QuoteSnap";

    public string? ReplyToEmail { get; set; }
}