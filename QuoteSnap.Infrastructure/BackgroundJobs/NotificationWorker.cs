using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuoteSnap.Application.Email;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.BackgroundJobs;

public class NotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationWorker> _logger;

    private static readonly TimeSpan PollInterval =
        TimeSpan.FromSeconds(30);

    private const int MaxRetryCount = 3;

    public NotificationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "QuoteSnap notification worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while processing notifications.");
            }

            await Task.Delay(
                PollInterval,
                stoppingToken);
        }
    }

    private async Task ProcessNotificationsAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var emailService =
            scope.ServiceProvider
                .GetRequiredService<ISystemEmailService>();

        var now = DateTime.UtcNow;

        var notifications =
            await dbContext.Notifications
                .Where(x =>
                    x.Status == NotificationStatus.Pending &&
                    x.ScheduledFor <= now)
                .OrderBy(x => x.ScheduledFor)
                .Take(20)
                .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            notification.Status =
                NotificationStatus.Processing;

            notification.ProcessingStartedAt =
                DateTime.UtcNow;

            notification.UpdatedAt =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync(
                cancellationToken);

            try
            {
                var result =
                    await emailService.SendAsync(
                        new EmailMessage
                        {
                            To = notification.Recipient,
                            Subject = notification.Subject,
                            HtmlBody = notification.HtmlBody
                        },
                        cancellationToken);

                if (result.Success)
                {
                    notification.Status =
                        NotificationStatus.Sent;

                    notification.SentAt =
                        DateTime.UtcNow;

                    notification.Provider = "Brevo";

                    notification.ProviderMessageId =
                        result.ProviderMessageId;

                    notification.ErrorMessage = null;
                }
                else
                {
                    HandleFailure(
                        notification,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                HandleFailure(
                    notification,
                    ex.Message);
            }

            notification.UpdatedAt =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static void HandleFailure(
        Domain.Entities.Notification notification,
        string? errorMessage)
    {
        notification.RetryCount++;

        notification.ErrorMessage =
            LimitErrorMessage(errorMessage);

        notification.ProcessingStartedAt = null;

        if (notification.RetryCount >= MaxRetryCount)
        {
            notification.Status =
                NotificationStatus.Failed;

            notification.FailedAt =
                DateTime.UtcNow;

            return;
        }

        notification.Status =
            NotificationStatus.Pending;

        notification.ScheduledFor =
            DateTime.UtcNow.AddMinutes(
                GetRetryDelayMinutes(
                    notification.RetryCount));
    }

    private static int GetRetryDelayMinutes(
        int retryCount)
    {
        return retryCount switch
        {
            1 => 1,
            2 => 5,
            _ => 15
        };
    }

    private static string? LimitErrorMessage(
        string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return null;

        return errorMessage.Length <= 2000
            ? errorMessage
            : errorMessage[..2000];
    }
}