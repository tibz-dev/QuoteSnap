using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Email;

public class EmailConnectionDto
{
    public Guid Id { get; set; }

    public EmailProvider Provider { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime ConnectedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }
}