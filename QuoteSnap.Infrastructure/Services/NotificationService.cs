using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationService(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification> ScheduleAsync(
        Guid businessId,
        Guid? userId,
        NotificationType type,
        string recipient,
        string subject,
        string htmlBody,
        DateTime? scheduledFor = null,
        string? referenceType = null,
        Guid? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business ID is required.");

        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.");

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.");

        var notification = new Notification
        {
            BusinessId = businessId,
            UserId = userId,
            Type = type,
            Status = NotificationStatus.Pending,

            Recipient = recipient.Trim(),
            Subject = subject.Trim(),
            HtmlBody = htmlBody,

            ScheduledFor =
                scheduledFor ?? DateTime.UtcNow,

            ReferenceType = referenceType,
            ReferenceId = referenceId
        };

        _dbContext.Notifications.Add(notification);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return notification;
    }
}