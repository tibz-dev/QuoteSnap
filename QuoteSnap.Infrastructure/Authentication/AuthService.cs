using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuoteSnap.Application.Authentication;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Identity;
using QuoteSnap.Infrastructure.Persistence;
using QuoteSnap.Infrastructure.Services;
using QuoteSnap.Infrastructure.Subscriptions;

namespace QuoteSnap.Infrastructure.Authentication;

public class AuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly NotificationService _notificationService;
    private readonly SubscriptionSettings _subscriptionSettings;

    public AuthService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        JwtTokenService jwtTokenService, 
        NotificationService notificationService,
        IOptions<SubscriptionSettings> subscriptionOptions)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _notificationService = notificationService;
        _subscriptionSettings = subscriptionOptions.Value;

    }

    public async Task VerifyEmailAsync(
    VerifyEmailRequest request)
    {
        var user =
            await _userManager.FindByIdAsync(
                request.UserId.ToString());

        if (user is null)
        {
            throw new InvalidOperationException(
                "Invalid email verification request.");
        }

        if (user.EmailConfirmed)
            return;

        var result =
            await _userManager.ConfirmEmailAsync(
                user,
                request.Token);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "The email verification link is invalid or has expired.");
        }
    }

    public async Task ResendVerificationEmailAsync(
        ResendVerificationEmailRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(email);

        // Do not reveal whether an account exists.
        if (user is null || user.EmailConfirmed)
            return;

        await QueueVerificationEmailAsync(user);
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "An account with this email already exists.");
        }

        var business = new Business
        {
            Name = request.BusinessName.Trim(),

            CountryCode = request.CountryCode
                .Trim()
                .ToUpperInvariant(),

            CurrencyCode = request.CurrencyCode
                .Trim()
                .ToUpperInvariant(),

            SubscriptionPlan = SubscriptionPlan.Free,

            QuotePrefix = "QT",
            NextQuoteNumber = 1,

            InvoicePrefix = "INV",
            NextInvoiceNumber = 1,

            ReceiptPrefix = "RCT",
            NextReceiptNumber = 1,

            DefaultQuoteValidityDays = 7
        };

        var now = DateTime.UtcNow;

        var subscription = new Subscription
        {
            BusinessId = business.Id,

            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Trial,

            TrialStartedAt = now,
            TrialEndsAt = now.AddDays(_subscriptionSettings.TrialDays),
        };

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,

            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),

            BusinessId = business.Id,

            IsActive = true
        };

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            _dbContext.Businesses.Add(business);
            _dbContext.Subscriptions.Add(subscription);

            await _dbContext.SaveChangesAsync();

            var userResult =
                await _userManager.CreateAsync(
                    user,
                    request.Password);

            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        " ",
                        userResult.Errors.Select(
                            x => x.Description)));
            }

            const string ownerRole = "Owner";

            if (!await _roleManager.RoleExistsAsync(ownerRole))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole<Guid>(ownerRole));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            " ",
                            roleResult.Errors.Select(
                                x => x.Description)));
                }
            }

            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    ownerRole);

            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        " ",
                        addRoleResult.Errors.Select(
                            x => x.Description)));
            }

            var welcomeNotification = new Notification
            {
                BusinessId = business.Id,
                UserId = user.Id,

                Type = NotificationType.Welcome,
                Status = NotificationStatus.Pending,

                Recipient = email,

                Subject = "Welcome to QuoteSnap",

                HtmlBody = BuildWelcomeEmail(
                    user.FirstName,
                    business.Name),

                ScheduledFor = DateTime.UtcNow,

                ReferenceType = "User",
                ReferenceId = user.Id
            };

            _dbContext.Notifications.Add(
                welcomeNotification);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            await QueueTrialNotificationsAsync(user,business,subscription);

            await QueueVerificationEmailAsync(user);

            return await _jwtTokenService
                .CreateTokenAsync(user);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task QueueTrialNotificationsAsync(
    ApplicationUser user,
    Business business,
    Subscription subscription)
    {
        if (string.IsNullOrWhiteSpace(user.Email) ||
            subscription.TrialEndsAt is null)
        {
            return;
        }

        var trialEndsAt = subscription.TrialEndsAt.Value;

        await _notificationService.ScheduleAsync(
            business.Id,
            user.Id,
            NotificationType.TrialStarted,
            user.Email,
            "Your QuoteSnap trial has started",
            BuildTrialEmail(
                user.FirstName,
                "Your QuoteSnap trial has started.",
                $"Your trial ends on {trialEndsAt:dd MMMM yyyy}."),
            DateTime.UtcNow,
            "Subscription",
            subscription.Id);

        await ScheduleTrialReminderAsync(
            user,
            business,
            subscription,
            trialEndsAt.AddDays(-7),
            "7 days");

        await ScheduleTrialReminderAsync(
            user,
            business,
            subscription,
            trialEndsAt.AddDays(-3),
            "3 days");

        await ScheduleTrialReminderAsync(
            user,
            business,
            subscription,
            trialEndsAt.AddDays(-1),
            "1 day");

        await _notificationService.ScheduleAsync(
            business.Id,
            user.Id,
            NotificationType.TrialEnded,
            user.Email,
            "Your QuoteSnap trial has ended",
            BuildTrialEmail(
                user.FirstName,
                "Your QuoteSnap trial has ended.",
                "You can choose a plan to continue using paid QuoteSnap features."),
            trialEndsAt,
            "Subscription",
            subscription.Id);
    }

    private async Task ScheduleTrialReminderAsync(
    ApplicationUser user,
    Business business,
    Subscription subscription,
    DateTime scheduledFor,
    string timeRemaining)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            return;

        if (scheduledFor <= DateTime.UtcNow)
            return;

        await _notificationService.ScheduleAsync(
            business.Id,
            user.Id,
            NotificationType.TrialEndingSoon,
            user.Email,
            $"Your QuoteSnap trial ends in {timeRemaining}",
            BuildTrialEmail(
                user.FirstName,
                $"Your QuoteSnap trial ends in {timeRemaining}.",
                "Choose a plan before your trial expires to avoid interruption."),
            scheduledFor,
            "Subscription",
            subscription.Id);
    }

    private static string BuildTrialEmail(
        string firstName,
        string heading,
        string message)
    {
        var safeName =
            System.Net.WebUtility.HtmlEncode(firstName);

        var safeHeading =
            System.Net.WebUtility.HtmlEncode(heading);

        var safeMessage =
            System.Net.WebUtility.HtmlEncode(message);

        return $"""
        <div style="
            font-family:Arial,sans-serif;
            line-height:1.6;
            max-width:600px;
            margin:auto;">

            <h2>{safeHeading}</h2>

            <p>Hi {safeName},</p>

            <p>{safeMessage}</p>

            <p>
                Regards,<br />
                <strong>QuoteSnap</strong>
            </p>

        </div>
        """;
    }

    public async Task ForgotPasswordAsync(
    ForgotPasswordRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(email);

        // Never reveal whether an email is registered.
        if (user is null || !user.IsActive)
            return;

        var token =
            await _userManager
                .GeneratePasswordResetTokenAsync(user);

        var encodedToken =
            Uri.EscapeDataString(token);

        var resetUrl =
            $"https://localhost:7011/api/auth/reset-password" +
            $"?userId={user.Id}" +
            $"&token={encodedToken}";

        var safeFirstName =
            System.Net.WebUtility.HtmlEncode(
                user.FirstName);

        var safeResetUrl =
            System.Net.WebUtility.HtmlEncode(
                resetUrl);

        var html = $"""
        <div style="
            font-family:Arial,sans-serif;
            line-height:1.6;
            max-width:600px;
            margin:auto;">

            <h2>Reset your QuoteSnap password</h2>

            <p>Hi {safeFirstName},</p>

            <p>
                We received a request to reset your
                QuoteSnap password.
            </p>

            <p>
                <a href="{safeResetUrl}">
                    Reset my password
                </a>
            </p>

            <p>
                If you didn't request a password reset,
                you can ignore this email.
            </p>

            <p>
                Regards,<br />
                <strong>QuoteSnap</strong>
            </p>

        </div>
        """;

        await _notificationService.ScheduleAsync(
            user.BusinessId,
            user.Id,
            NotificationType.PasswordReset,
            user.Email!,
            "Reset your QuoteSnap password",
            html,
            referenceType: "User",
            referenceId: user.Id);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request)
    {
        var user =
            await _userManager.FindByIdAsync(
                request.UserId.ToString());

        if (user is null)
        {
            throw new InvalidOperationException(
                "The password reset request is invalid or has expired.");
        }

        var result =
            await _userManager.ResetPasswordAsync(
                user,
                request.Token,
                request.NewPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(
                    " ",
                    result.Errors.Select(
                        x => x.Description)));
        }
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "This account is inactive.");
        }

        var validPassword =
            await _userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!validPassword)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        return await _jwtTokenService
            .CreateTokenAsync(user);
    }

    private async Task QueueVerificationEmailAsync(
    ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            return;

        var token =
            await _userManager
                .GenerateEmailConfirmationTokenAsync(user);

        var encodedToken =
            Uri.EscapeDataString(token);

        var verificationUrl =
            $"https://localhost:7011/api/auth/verify-email" +
            $"?userId={user.Id}" +
            $"&token={encodedToken}";

        var safeFirstName =
            System.Net.WebUtility.HtmlEncode(
                user.FirstName);

        var safeVerificationUrl =
            System.Net.WebUtility.HtmlEncode(
                verificationUrl);

        var html = $"""
        <div style="
            font-family:Arial,sans-serif;
            line-height:1.6;
            max-width:600px;
            margin:auto;">

            <h2>Verify your QuoteSnap email</h2>

            <p>Hi {safeFirstName},</p>

            <p>
                Please verify your email address to complete
                your QuoteSnap account setup.
            </p>

            <p>
                <a href="{safeVerificationUrl}">
                    Verify my email
                </a>
            </p>

            <p>
                If you did not create a QuoteSnap account,
                you can ignore this email.
            </p>

            <p>
                Regards,<br />
                <strong>QuoteSnap</strong>
            </p>

        </div>
        """;

        await _notificationService.ScheduleAsync(
            user.BusinessId,
            user.Id,
            NotificationType.EmailVerification,
            user.Email,
            "Verify your QuoteSnap email",
            html,
            referenceType: "User",
            referenceId: user.Id);
    }
    private static string BuildWelcomeEmail(
        string firstName,
        string businessName)
    {
        var safeFirstName =
            System.Net.WebUtility.HtmlEncode(firstName);

        var safeBusinessName =
            System.Net.WebUtility.HtmlEncode(businessName);

        return $"""
            <div style="
                font-family:Arial,sans-serif;
                line-height:1.6;
                max-width:600px;
                margin:auto;">

                <h2>Welcome to QuoteSnap 👋</h2>

                <p>Hi {safeFirstName},</p>

                <p>
                    Welcome to QuoteSnap. Your account for
                    <strong>{safeBusinessName}</strong>
                    has been created successfully.
                </p>

                <p>
                    You can now start setting up your business,
                    adding customers and catalogue items, and
                    creating professional quotes and invoices.
                </p>

                <p>
                    Regards,<br />
                    <strong>QuoteSnap</strong>
                </p>

            </div>
            """;
    }
}