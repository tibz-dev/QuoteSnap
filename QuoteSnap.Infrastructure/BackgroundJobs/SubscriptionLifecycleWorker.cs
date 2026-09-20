using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.BackgroundJobs;

public class SubscriptionLifecycleWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionLifecycleWorker> _logger;

    private static readonly TimeSpan PollInterval =
        TimeSpan.FromMinutes(5);

    public SubscriptionLifecycleWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionLifecycleWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "QuoteSnap subscription lifecycle worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptionsAsync(
                    stoppingToken);
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
                    "An error occurred while processing subscriptions.");
            }

            await Task.Delay(
                PollInterval,
                stoppingToken);
        }
    }

    private async Task ProcessSubscriptionsAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var expiredTrials =
            await dbContext.Subscriptions
                .Where(x =>
                    x.Status == SubscriptionStatus.Trial &&
                    x.TrialEndsAt.HasValue &&
                    x.TrialEndsAt.Value <= now)
                .ToListAsync(cancellationToken);

        foreach (var subscription in expiredTrials)
        {
            subscription.Status =
                SubscriptionStatus.Expired;

            subscription.EndedAt = now;
            subscription.UpdatedAt = now;
        }

        var expiredSubscriptions =
            await dbContext.Subscriptions
                .Where(x =>
                    x.Status == SubscriptionStatus.Active &&
                    x.CurrentPeriodEndsAt.HasValue &&
                    x.CurrentPeriodEndsAt.Value <= now)
                .ToListAsync(cancellationToken);

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status =
                SubscriptionStatus.Expired;

            subscription.EndedAt = now;
            subscription.UpdatedAt = now;
        }

        if (expiredTrials.Count > 0 ||
            expiredSubscriptions.Count > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }
}