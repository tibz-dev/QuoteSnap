using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Subscriptions;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Identity;
using QuoteSnap.Infrastructure.Payments.Paystack;
using QuoteSnap.Infrastructure.Persistence;
using QuoteSnap.Infrastructure.Services;
using QuoteSnap.Infrastructure.Subscriptions;

namespace QuoteSnap.Infrastructure.Payments;

public class SubscriptionPaymentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PaystackService _paystackService;
    private readonly SubscriptionSettings _settings;
    private readonly NotificationService _notificationService;

    public SubscriptionPaymentService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager,
        PaystackService paystackService,
        IOptions<SubscriptionSettings> options,
        NotificationService notificationService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _userManager = userManager;
        _paystackService = paystackService;
        _notificationService = notificationService;
        _settings = options.Value;
    }


    public async Task<SubscriptionPaymentDto> GetAsync(
    Guid paymentId,
    CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();

        var payment =
            await _dbContext.SubscriptionPayments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == paymentId &&
                        x.BusinessId == businessId,
                    cancellationToken);

        if (payment is null)
        {
            throw new InvalidOperationException(
                "Subscription payment could not be found.");
        }

        return new SubscriptionPaymentDto
        {
            Id = payment.Id,
            Plan = payment.Plan,
            Amount = payment.Amount,
            CurrencyCode = payment.CurrencyCode,
            Status = payment.Status,
            Reference = payment.ExternalReference,
            PaidAt = payment.PaidAt,
            FailedAt = payment.FailedAt,
            FailureReason = payment.FailureReason,
            CreatedAt = payment.CreatedAt
        };
    }

    public async Task<InitializeSubscriptionPaymentResponse>
        InitializeAsync(
            InitializeSubscriptionPaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        var businessId = GetBusinessId();

        if (request.Plan != SubscriptionPlan.Pro &&
            request.Plan != SubscriptionPlan.Business)
        {
            throw new ArgumentException(
                "A paid subscription plan is required.");
        }

        var subscription =
            await _dbContext.Subscriptions
                .FirstOrDefaultAsync(
                    x => x.BusinessId == businessId,
                    cancellationToken);

        if (subscription is null)
        {
            throw new InvalidOperationException(
                "Subscription could not be found.");
        }

        var userId = _currentUser.UserId;

        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "User information is missing.");
        }

        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException(
                "Account email could not be found.");
        }

        var amount =
            GetPlanPrice(request.Plan);

        var currency =
            _settings.BillingCurrency
                .Trim()
                .ToUpperInvariant();

        var payment = new SubscriptionPayment
        {
            BusinessId = businessId,
            SubscriptionId = subscription.Id,
            Plan = request.Plan,
            Amount = amount,
            CurrencyCode = currency,
            Status = SubscriptionPaymentStatus.Pending,
            PaymentProvider = "Paystack"
        };

        var reference =
            $"QS-{payment.Id:N}";

        payment.ExternalReference = reference;

        _dbContext.SubscriptionPayments.Add(payment);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        try
        {
            var result =
                await _paystackService
                    .InitializeTransactionAsync(
                        user.Email,
                        amount,
                        currency,
                        reference,
                        cancellationToken);

            payment.ExternalReference =
                result.Reference;

            payment.UpdatedAt =
                DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new InitializeSubscriptionPaymentResponse
            {
                PaymentId = payment.Id,
                Reference = result.Reference,
                AuthorizationUrl =
                    result.AuthorizationUrl,
                AccessCode =
                    result.AccessCode
            };
        }
        catch
        {
            payment.Status =
                SubscriptionPaymentStatus.Failed;

            payment.FailedAt =
                DateTime.UtcNow;

            payment.FailureReason =
                "Payment initialization failed.";

            payment.UpdatedAt =
                DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                CancellationToken.None);

            throw;
        }
    }

    public async Task ProcessSuccessfulPaymentAsync(
    string reference,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException(
                "Payment reference is required.");
        }

        var payment =
            await _dbContext.SubscriptionPayments
                .Include(x => x.Subscription)
                .Include(x => x.Business)
                .FirstOrDefaultAsync(
                    x =>
                        x.ExternalReference == reference &&
                        x.PaymentProvider == "Paystack",
                    cancellationToken);

        if (payment is null)
        {
            throw new InvalidOperationException(
                "Subscription payment could not be found.");
        }

        // Idempotency:
        // Paystack may deliver the same webhook more than once.
        if (payment.Status ==
            SubscriptionPaymentStatus.Successful)
        {
            return;
        }

        var verification =
            await _paystackService.VerifyTransactionAsync(
                reference,
                cancellationToken);

        if (!string.Equals(
                verification.Status,
                "success",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Paystack transaction is not successful.");
        }

        if (!string.Equals(
                verification.Reference,
                payment.ExternalReference,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Payment reference does not match.");
        }

        var expectedAmount =
            decimal.ToInt64(
                decimal.Round(
                    payment.Amount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

        if (verification.Amount != expectedAmount)
        {
            throw new InvalidOperationException(
                "Payment amount does not match.");
        }

        if (!string.Equals(
                verification.Currency,
                payment.CurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Payment currency does not match.");
        }

        var now = DateTime.UtcNow;

        payment.Status =
            SubscriptionPaymentStatus.Successful;

        payment.ExternalPaymentId =
            verification.Id.ToString();

        payment.PaidAt =
            verification.PaidAt ?? now;

        payment.FailedAt = null;
        payment.FailureReason = null;
        payment.UpdatedAt = now;

        var subscription = payment.Subscription;

        subscription.Plan = payment.Plan;
        subscription.Status = SubscriptionStatus.Active;

        subscription.CurrentPeriodStartsAt = now;
        subscription.CurrentPeriodEndsAt = now.AddMonths(1);

        subscription.EndedAt = null;
        subscription.CancelledAt = null;
        subscription.UpdatedAt = now;

        payment.Business.SubscriptionPlan =
            payment.Plan;

        payment.Business.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private decimal GetPlanPrice(
        SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Pro =>
                _settings.ProMonthlyPrice,

            SubscriptionPlan.Business =>
                _settings.BusinessMonthlyPrice,

            _ => throw new ArgumentException(
                "Unsupported subscription plan.")
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