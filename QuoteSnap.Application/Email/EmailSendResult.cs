namespace QuoteSnap.Application.Email;

public class EmailSendResult
{
    public bool Success { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? ErrorMessage { get; set; }
}