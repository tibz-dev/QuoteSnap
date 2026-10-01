using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Subscriptions;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Subscriptions;

public class SubscriptionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly SubscriptionSettings _settings;

    public SubscriptionService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IOptions<SubscriptionSettings> options)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _settings = options.Value;
    }

    public async Task<SubscriptionDto> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var subscription =
            await GetOrCreateCurrentEntityAsync(
                cancellationToken);

        return Map(subscription);
    }

    public async Task<Subscription> GetOrCreateCurrentEntityAsync(
        CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();

        var subscription =
            await _dbContext.Subscriptions
                .FirstOrDefaultAsync(
                    x => x.BusinessId == businessId,
                    cancellationToken);

        if (subscription is not null)
        {
            return subscription;
        }

        var business =
            await _dbContext.Businesses
                .FirstOrDefaultAsync(
                    x => x.Id == businessId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Business could not be found.");

        var now = DateTime.UtcNow;

        subscription = new Subscription
        {
            BusinessId = businessId,
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Trial,
            TrialStartedAt = now,
            TrialEndsAt = now.AddDays(_settings.TrialDays)
        };

        business.SubscriptionPlan = SubscriptionPlan.Free;
        business.UpdatedAt = now;

        _dbContext.Subscriptions.Add(subscription);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(subscription).State =
                EntityState.Detached;

            subscription =
                await _dbContext.Subscriptions
                    .FirstOrDefaultAsync(
                        x => x.BusinessId == businessId,
                        cancellationToken)
                ?? throw;
        }

        return subscription;
    }

    private static SubscriptionDto Map(
        Subscription subscription)
    {
        int? trialDaysRemaining = null;

        if (subscription.TrialEndsAt.HasValue)
        {
            var remaining =
                subscription.TrialEndsAt.Value -
                DateTime.UtcNow;

            trialDaysRemaining =
                Math.Max(
                    0,
                    (int)Math.Ceiling(
                        remaining.TotalDays));
        }

        return new SubscriptionDto
        {
            Id = subscription.Id,
            Plan = subscription.Plan,
            Status = subscription.Status,
            TrialStartedAt = subscription.TrialStartedAt,
            TrialEndsAt = subscription.TrialEndsAt,
            TrialDaysRemaining = trialDaysRemaining,
            CurrentPeriodStartsAt = subscription.CurrentPeriodStartsAt,
            CurrentPeriodEndsAt = subscription.CurrentPeriodEndsAt,
            CancelledAt = subscription.CancelledAt,
            EndedAt = subscription.EndedAt
        };
    }

    private Guid GetBusinessId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.BusinessId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Business information is missing.");
        }

        return _currentUser.BusinessId;
    }
}
