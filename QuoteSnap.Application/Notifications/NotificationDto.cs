namespace QuoteSnap.Application.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }

    public int Type { get; set; }

    public int Status { get; set; }

    public string Recipient { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTime ScheduledFor { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; }
}
