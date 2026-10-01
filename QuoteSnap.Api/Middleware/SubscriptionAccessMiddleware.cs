using QuoteSnap.Infrastructure.Subscriptions;

namespace QuoteSnap.Api.Middleware;

public class SubscriptionAccessMiddleware
{
    private readonly RequestDelegate _next;

    public SubscriptionAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        SubscriptionEntitlementService entitlements)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path =
            (context.Request.Path.Value ?? string.Empty)
            .TrimEnd('/')
            .ToLowerInvariant();

        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        try
        {
            if (IsEmailDeliveryAction(path, context.Request.Method))
            {
                await entitlements.EnsureFeatureAsync(
                    SubscriptionFeature.EmailDelivery,
                    context.RequestAborted);

                await _next(context);
                return;
            }

            if (IsProtectedMutation(path, context.Request.Method))
            {
                await entitlements.EnsureWriteAccessAsync(
                    context.RequestAborted);

                var resource = GetCreateResource(
                    path,
                    context.Request.Method);

                if (resource.HasValue)
                {
                    await entitlements.EnsureResourceLimitAsync(
                        resource.Value,
                        context.RequestAborted);
                }
            }

            await _next(context);
        }
        catch (SubscriptionAccessException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(
                new
                {
                    code = ex.Code,
                    message = ex.Message
                },
                context.RequestAborted);
        }
    }

    private static bool ShouldSkip(string path)
    {
        return
            path.StartsWith("/api/auth") ||
            path.StartsWith("/api/subscription") ||
            path.StartsWith("/api/subscription-payments") ||
            path.StartsWith("/api/business") ||
            path.StartsWith("/api/notifications");
    }

    private static bool IsProtectedMutation(
        string path,
        string method)
    {
        if (!IsMutation(method))
        {
            return false;
        }

        return
            path.StartsWith("/api/customers") ||
            path.StartsWith("/api/categories") ||
            path.StartsWith("/api/catalogue-items") ||
            path.StartsWith("/api/quotes") ||
            path.StartsWith("/api/invoices") ||
            path.StartsWith("/api/receipts");
    }

    private static bool IsEmailDeliveryAction(
        string path,
        string method)
    {
        return
            (HttpMethods.IsPost(method) &&
             path.StartsWith("/api/invoices/") &&
             path.EndsWith("/send-email")) ||
            (HttpMethods.IsGet(method) &&
             path == "/api/email-connections/google/connect");
    }

    private static bool IsMutation(string method)
    {
        return
            HttpMethods.IsPost(method) ||
            HttpMethods.IsPut(method) ||
            HttpMethods.IsPatch(method) ||
            HttpMethods.IsDelete(method);
    }

    private static SubscriptionResource? GetCreateResource(
        string path,
        string method)
    {
        if (!HttpMethods.IsPost(method))
        {
            return null;
        }

        if (path == "/api/customers")
            return SubscriptionResource.Customers;

        if (path == "/api/catalogue-items")
            return SubscriptionResource.CatalogueItems;

        if (path == "/api/quotes")
            return SubscriptionResource.Quotes;

        if (path.StartsWith("/api/invoices/from-quote/"))
            return SubscriptionResource.Invoices;

        if (path.StartsWith("/api/receipts/from-payment/"))
            return SubscriptionResource.Receipts;

        return null;
    }
}
