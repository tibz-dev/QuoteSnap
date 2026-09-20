using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class EmailConnection : TenantEntity
{
    public EmailProvider Provider { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public string EncryptedRefreshToken { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public Business Business { get; set; } = null!;
}