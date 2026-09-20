using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Subscriptions;
using QuoteSnap.Infrastructure.Payments;
using QuoteSnap.Infrastructure.Payments.Paystack;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/subscription-payments")]
public class SubscriptionPaymentsController : ControllerBase
{
    private readonly SubscriptionPaymentService _paymentService;
    private readonly PaystackWebhookValidator _webhookValidator;

    public SubscriptionPaymentsController(
        SubscriptionPaymentService paymentService,
        PaystackWebhookValidator webhookValidator)
    {
        _paymentService = paymentService;
        _webhookValidator = webhookValidator;
    }

    [Authorize]
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize(
        [FromBody] InitializeSubscriptionPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _paymentService.InitializeAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPayment(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment =
                await _paymentService.GetAsync(
                    paymentId,
                    cancellationToken);

            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        CancellationToken cancellationToken)
    {
        using var reader =
            new StreamReader(Request.Body);

        var payload =
            await reader.ReadToEndAsync(
                cancellationToken);

        var signature =
            Request.Headers["x-paystack-signature"]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(signature) ||
            !_webhookValidator.IsValid(
                payload,
                signature))
        {
            return Unauthorized();
        }

        using var document =
            JsonDocument.Parse(payload);

        var root =
            document.RootElement;

        if (!root.TryGetProperty(
                "event",
                out var eventElement))
        {
            return Ok();
        }

        if (!root.TryGetProperty(
                "data",
                out var dataElement))
        {
            return Ok();
        }

        var eventName =
            eventElement.GetString();

        if (string.Equals(
                eventName,
                "charge.success",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandleChargeSuccessAsync(
                dataElement,
                cancellationToken);

            return Ok();
        }

        if (string.Equals(
                eventName,
                "subscription.create",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandleSubscriptionCreatedAsync(
                dataElement,
                cancellationToken);

            return Ok();
        }
        if (string.Equals(
        eventName,
        "invoice.update",
        StringComparison.OrdinalIgnoreCase))
        {
            if (!dataElement.TryGetProperty(
                    "paid",
                    out var paidElement) ||
                paidElement.ValueKind != JsonValueKind.True)
            {
                return Ok();
            }

            var subscriptionCode =
                GetSubscriptionCode(dataElement);

            if (!string.IsNullOrWhiteSpace(
                    subscriptionCode))
            {
                await _paymentService
                    .ProcessSubscriptionRenewedAsync(
                        subscriptionCode,
                        cancellationToken);
            }

            return Ok();
        }

        if (string.Equals(
                eventName,
                "subscription.disable",
                StringComparison.OrdinalIgnoreCase))
        {
            var subscriptionCode =
                GetSubscriptionCode(dataElement);

            if (!string.IsNullOrWhiteSpace(
                    subscriptionCode))
            {
                await _paymentService
                    .ProcessSubscriptionDisabledAsync(
                        subscriptionCode,
                        cancellationToken);
            }

            return Ok();
        }

        return Ok();
    }

    private async Task HandleChargeSuccessAsync(
        JsonElement dataElement,
        CancellationToken cancellationToken)
    {
        if (!dataElement.TryGetProperty(
                "reference",
                out var referenceElement))
        {
            return;
        }

        var reference =
            referenceElement.GetString();

        if (string.IsNullOrWhiteSpace(reference))
            return;

        await _paymentService
            .ProcessSuccessfulPaymentAsync(
                reference,
                cancellationToken);
    }

    private async Task HandleSubscriptionCreatedAsync(
        JsonElement dataElement,
        CancellationToken cancellationToken)
    {
        if (!dataElement.TryGetProperty(
                "subscription_code",
                out var subscriptionCodeElement))
        {
            return;
        }

        if (!dataElement.TryGetProperty(
        "email_token",
        out var emailTokenElement))
        {
            return;
        }

        if (!dataElement.TryGetProperty(
                "customer",
                out var customerElement))
        {
            return;
        }

        if (!customerElement.TryGetProperty(
                "customer_code",
                out var customerCodeElement))
        {
            return;
        }

        if (!customerElement.TryGetProperty(
                "email",
                out var emailElement))
        {
            return;
        }

        var subscriptionCode =
            subscriptionCodeElement.GetString();

        var emailToken =
    emailTokenElement.GetString();

        var customerCode =
            customerCodeElement.GetString();

        var customerEmail =
            emailElement.GetString();

        if (string.IsNullOrWhiteSpace(subscriptionCode) ||
            string.IsNullOrWhiteSpace(customerCode) ||
            string.IsNullOrWhiteSpace(customerEmail) ||
            string.IsNullOrWhiteSpace(emailToken))
        {
            return;
        }

        await _paymentService
            .ProcessSubscriptionCreatedAsync(
                customerEmail,
                customerCode,
                subscriptionCode,
                emailToken,
                cancellationToken);
    }

    private static string? GetSubscriptionCode(
    JsonElement dataElement)
    {
        if (dataElement.TryGetProperty(
                "subscription_code",
                out var directCode))
        {
            return directCode.GetString();
        }

        if (dataElement.TryGetProperty(
                "subscription",
                out var subscriptionElement) &&
            subscriptionElement.ValueKind ==
                JsonValueKind.Object &&
            subscriptionElement.TryGetProperty(
                "subscription_code",
                out var nestedCode))
        {
            return nestedCode.GetString();
        }

        return null;
    }
}