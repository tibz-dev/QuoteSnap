namespace QuoteSnap.Infrastructure.Subscriptions;

public class SubscriptionAccessException : Exception
{
    public SubscriptionAccessException(
        string code,
        string message,
        int statusCode = 402)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public int StatusCode { get; }
}
