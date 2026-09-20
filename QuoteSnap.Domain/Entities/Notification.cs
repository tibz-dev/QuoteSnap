using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class Notification : TenantEntity
{
    public Guid? UserId { get; set; }

    public NotificationType Type { get; set; }

    public NotificationStatus Status { get; set; }
        = NotificationStatus.Pending;

    public string Recipient { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    public DateTime ScheduledFor { get; set; }

    public DateTime? ProcessingStartedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public int RetryCount { get; set; }

    public string? Provider { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Business Business { get; set; } = null!;
}