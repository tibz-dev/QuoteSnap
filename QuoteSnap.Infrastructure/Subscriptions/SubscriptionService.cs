using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Subscriptions;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Subscriptions;

public class SubscriptionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public SubscriptionService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<SubscriptionDto> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();

        var subscription =
            await _dbContext.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BusinessId == businessId,
                    cancellationToken);

        if (subscription is null)
        {
            throw new InvalidOperationException(
                "Subscription could not be found.");
        }

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

            TrialStartedAt =
                subscription.TrialStartedAt,

            TrialEndsAt =
                subscription.TrialEndsAt,

            TrialDaysRemaining =
                trialDaysRemaining,

            CurrentPeriodStartsAt =
                subscription.CurrentPeriodStartsAt,

            CurrentPeriodEndsAt =
                subscription.CurrentPeriodEndsAt,

            CancelledAt =
                subscription.CancelledAt,

            EndedAt =
                subscription.EndedAt
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