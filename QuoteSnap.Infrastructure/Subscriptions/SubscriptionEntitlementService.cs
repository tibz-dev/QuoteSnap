using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Subscriptions;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Subscriptions;

public enum SubscriptionResource
{
    Customers,
    CatalogueItems,
    Quotes,
    Invoices,
    Receipts
}

public enum SubscriptionFeature
{
    EmailDelivery
}

public class SubscriptionEntitlementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly SubscriptionSettings _settings;
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionEntitlementService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IOptions<SubscriptionSettings> options,
        SubscriptionService subscriptionService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _settings = options.Value;
        _subscriptionService = subscriptionService;
    }

    public async Task<SubscriptionOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();
        var subscription = await GetSubscriptionAsync(
            businessId,
            cancellationToken);

        var access = GetAccessState(subscription);
        var effectivePlan = ResolveEffectivePlan(subscription);

        return new SubscriptionOverviewDto
        {
            Subscription = MapSubscription(subscription),
            EffectivePlan = GetPlanDefinition(effectivePlan),
            Plans = new List<SubscriptionPlanDefinitionDto>
            {
                GetPlanDefinition(SubscriptionPlan.Free),
                GetPlanDefinition(SubscriptionPlan.Pro),
                GetPlanDefinition(SubscriptionPlan.Business)
            },
            Usage = await GetUsageAsync(
                businessId,
                cancellationToken),
            CanWrite = access.CanWrite,
            IsReadOnly = !access.CanWrite,
            IsTrialUsingProAccess =
                IsActiveTrial(subscription) &&
                _settings.TrialUsesProAccess,
            AccessMessage = access.Message
        };
    }

    public async Task EnsureWriteAccessAsync(
        CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();
        var subscription = await GetSubscriptionAsync(
            businessId,
            cancellationToken);

        var access = GetAccessState(subscription);

        if (!access.CanWrite)
        {
            throw new SubscriptionAccessException(
                "subscription_write_blocked",
                access.Message ??
                "Your subscription does not currently allow changes.");
        }
    }

    public async Task EnsureFeatureAsync(
        SubscriptionFeature feature,
        CancellationToken cancellationToken = default)
    {
        await EnsureWriteAccessAsync(cancellationToken);

        var businessId = GetBusinessId();
        var subscription = await GetSubscriptionAsync(
            businessId,
            cancellationToken);

        var effectivePlan = ResolveEffectivePlan(subscription);

        var plan = GetPlanDefinition(effectivePlan);

        var allowed = feature switch
        {
            SubscriptionFeature.EmailDelivery =>
                plan.EmailDelivery,
            _ => false
        };

        if (!allowed)
        {
            throw new SubscriptionAccessException(
                "feature_not_in_plan",
                "Email delivery is available on Pro and Business plans.");
        }
    }

    public async Task EnsureResourceLimitAsync(
        SubscriptionResource resource,
        CancellationToken cancellationToken = default)
    {
        await EnsureWriteAccessAsync(cancellationToken);

        var businessId = GetBusinessId();
        var subscription = await GetSubscriptionAsync(
            businessId,
            cancellationToken);

        var effectivePlan = ResolveEffectivePlan(subscription);

        var plan = GetPlanDefinition(effectivePlan);
        var usage = await GetUsageAsync(
            businessId,
            cancellationToken);

        var (used, limit, label) = resource switch
        {
            SubscriptionResource.Customers =>
                (usage.Customers, plan.CustomerLimit, "customer"),
            SubscriptionResource.CatalogueItems =>
                (usage.CatalogueItems, plan.CatalogueItemLimit, "catalogue item"),
            SubscriptionResource.Quotes =>
                (usage.QuotesThisMonth, plan.MonthlyQuoteLimit, "monthly quote"),
            SubscriptionResource.Invoices =>
                (usage.InvoicesThisMonth, plan.MonthlyInvoiceLimit, "monthly invoice"),
            SubscriptionResource.Receipts =>
                (usage.ReceiptsThisMonth, plan.MonthlyReceiptLimit, "monthly receipt"),
            _ => (0, null, "resource")
        };

        if (limit.HasValue && used >= limit.Value)
        {
            throw new SubscriptionAccessException(
                "plan_limit_reached",
                $"Your {plan.Name} plan has reached its {label} limit of {limit.Value}. Upgrade your plan to continue.");
        }
    }

    private async Task<SubscriptionUsageDto> GetUsageAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        return new SubscriptionUsageDto
        {
            Customers = await _dbContext.Customers
                .CountAsync(
                    x =>
                        x.BusinessId == businessId &&
                        x.IsActive,
                    cancellationToken),

            CatalogueItems = await _dbContext.CatalogueItems
                .CountAsync(
                    x =>
                        x.BusinessId == businessId &&
                        x.IsActive,
                    cancellationToken),

            QuotesThisMonth = await _dbContext.Quotes
                .CountAsync(
                    x =>
                        x.BusinessId == businessId &&
                        x.CreatedAt >= monthStart,
                    cancellationToken),

            InvoicesThisMonth = await _dbContext.Invoices
                .CountAsync(
                    x =>
                        x.BusinessId == businessId &&
                        x.CreatedAt >= monthStart,
                    cancellationToken),

            ReceiptsThisMonth = await _dbContext.Receipts
                .CountAsync(
                    x =>
                        x.BusinessId == businessId &&
                        x.CreatedAt >= monthStart,
                    cancellationToken)
        };
    }

    private SubscriptionPlanDefinitionDto GetPlanDefinition(
        SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Free =>
                new SubscriptionPlanDefinitionDto
                {
                    Plan = SubscriptionPlan.Free,
                    Name = "Free",
                    MonthlyPrice = 0,
                    CurrencyCode = _settings.BillingCurrency,
                    CustomerLimit = _settings.FreeCustomerLimit,
                    CatalogueItemLimit = _settings.FreeCatalogueItemLimit,
                    MonthlyQuoteLimit = _settings.FreeMonthlyQuoteLimit,
                    MonthlyInvoiceLimit = _settings.FreeMonthlyInvoiceLimit,
                    MonthlyReceiptLimit = _settings.FreeMonthlyReceiptLimit,
                    EmailDelivery = false
                },

            SubscriptionPlan.Pro =>
                new SubscriptionPlanDefinitionDto
                {
                    Plan = SubscriptionPlan.Pro,
                    Name = "Pro",
                    MonthlyPrice = _settings.ProMonthlyPrice,
                    CurrencyCode = _settings.BillingCurrency,
                    CustomerLimit = _settings.ProCustomerLimit,
                    CatalogueItemLimit = _settings.ProCatalogueItemLimit,
                    MonthlyQuoteLimit = _settings.ProMonthlyQuoteLimit,
                    MonthlyInvoiceLimit = _settings.ProMonthlyInvoiceLimit,
                    MonthlyReceiptLimit = _settings.ProMonthlyReceiptLimit,
                    EmailDelivery = true
                },

            SubscriptionPlan.Business =>
                new SubscriptionPlanDefinitionDto
                {
                    Plan = SubscriptionPlan.Business,
                    Name = "Business",
                    MonthlyPrice = _settings.BusinessMonthlyPrice,
                    CurrencyCode = _settings.BillingCurrency,
                    CustomerLimit = null,
                    CatalogueItemLimit = null,
                    MonthlyQuoteLimit = null,
                    MonthlyInvoiceLimit = null,
                    MonthlyReceiptLimit = null,
                    EmailDelivery = true
                },

            _ => throw new ArgumentOutOfRangeException(
                nameof(plan),
                plan,
                "Unknown subscription plan.")
        };
    }

    private static (bool CanWrite, string? Message) GetAccessState(
        Subscription subscription)
    {
        var now = DateTime.UtcNow;

        if (subscription.Status == SubscriptionStatus.PastDue)
        {
            return (
                false,
                "Your subscription payment is past due. Update billing or renew to continue making changes.");
        }

        if (subscription.Status == SubscriptionStatus.Cancelled &&
            subscription.CurrentPeriodEndsAt.HasValue &&
            subscription.CurrentPeriodEndsAt.Value > now)
        {
            return (
                true,
                $"Your subscription is cancelled and remains active until {subscription.CurrentPeriodEndsAt.Value:dd MMM yyyy}. After that, Free plan limits apply.");
        }

        if (subscription.Status == SubscriptionStatus.Trial &&
            !IsActiveTrial(subscription))
        {
            return (
                true,
                "Your trial has ended. Free plan limits now apply.");
        }

        if ((subscription.Status == SubscriptionStatus.Active ||
             subscription.Status == SubscriptionStatus.Cancelled ||
             subscription.Status == SubscriptionStatus.Expired) &&
            subscription.Plan != SubscriptionPlan.Free &&
            subscription.CurrentPeriodEndsAt.HasValue &&
            subscription.CurrentPeriodEndsAt.Value <= now)
        {
            return (
                true,
                "Your paid period has ended. Free plan limits now apply.");
        }

        return (true, null);
    }

    private SubscriptionPlan ResolveEffectivePlan(
        Subscription subscription)
    {
        if (IsActiveTrial(subscription) &&
            _settings.TrialUsesProAccess)
        {
            return SubscriptionPlan.Pro;
        }

        var now = DateTime.UtcNow;

        if (subscription.Status == SubscriptionStatus.Trial &&
            !IsActiveTrial(subscription))
        {
            return SubscriptionPlan.Free;
        }

        if ((subscription.Status == SubscriptionStatus.Cancelled ||
             subscription.Status == SubscriptionStatus.Expired ||
             subscription.Status == SubscriptionStatus.Active) &&
            subscription.Plan != SubscriptionPlan.Free &&
            subscription.CurrentPeriodEndsAt.HasValue &&
            subscription.CurrentPeriodEndsAt.Value <= now)
        {
            return SubscriptionPlan.Free;
        }

        return subscription.Plan;
    }

    private static bool IsActiveTrial(
        Subscription subscription)
    {
        return
            subscription.Status == SubscriptionStatus.Trial &&
            (!subscription.TrialEndsAt.HasValue ||
             subscription.TrialEndsAt.Value > DateTime.UtcNow);
    }

    private async Task<Subscription> GetSubscriptionAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var subscription =
            await _dbContext.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BusinessId == businessId,
                    cancellationToken);

        if (subscription is not null)
        {
            return subscription;
        }

        var created =
            await _subscriptionService
                .GetOrCreateCurrentEntityAsync(
                    cancellationToken);

        return created;
    }

    private static SubscriptionDto MapSubscription(
        Subscription subscription)
    {
        int? trialDaysRemaining = null;

        if (subscription.TrialEndsAt.HasValue)
        {
            trialDaysRemaining = Math.Max(
                0,
                (int)Math.Ceiling(
                    (subscription.TrialEndsAt.Value - DateTime.UtcNow)
                    .TotalDays));
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
