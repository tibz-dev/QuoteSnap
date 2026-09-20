namespace QuoteSnap.Application.Authentication;

public class VerifyEmailRequest
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;
}