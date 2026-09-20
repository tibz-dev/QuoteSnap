namespace QuoteSnap.Domain.Enums;

public enum NotificationType
{
    Welcome = 1,
    EmailVerification = 2,
    PasswordReset = 3,

    TrialStarted = 10,
    TrialEndingSoon = 11,
    TrialEnded = 12,

    SubscriptionActivated = 20,
    SubscriptionRenewed = 21,
    SubscriptionEndingSoon = 22,
    SubscriptionExpired = 23,

    PaymentSuccessful = 30,
    PaymentFailed = 31,

    PostExpiryReminder = 40
}