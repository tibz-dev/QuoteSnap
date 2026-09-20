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
            Request.Headers[
                "x-paystack-signature"]
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

        var eventName =
            eventElement.GetString();

        if (!string.Equals(
                eventName,
                "charge.success",
                StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        if (!root.TryGetProperty(
                "data",
                out var dataElement) ||
            !dataElement.TryGetProperty(
                "reference",
                out var referenceElement))
        {
            return Ok();
        }

        var reference =
            referenceElement.GetString();

        if (string.IsNullOrWhiteSpace(reference))
            return Ok();

        await _paymentService
            .ProcessSuccessfulPaymentAsync(
                reference,
                cancellationToken);

        return Ok();
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
}