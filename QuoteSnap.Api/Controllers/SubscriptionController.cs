using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Subscriptions;
using QuoteSnap.Infrastructure.Payments;
using QuoteSnap.Infrastructure.Subscriptions;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;
    private readonly SubscriptionEntitlementService _entitlements;
    private readonly SubscriptionPaymentService _payments;

    public SubscriptionController(
        SubscriptionService subscriptionService,
        SubscriptionEntitlementService entitlements,
        SubscriptionPaymentService payments)
    {
        _subscriptionService = subscriptionService;
        _entitlements = entitlements;
        _payments = payments;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrent(
        CancellationToken cancellationToken)
    {
        try
        {
            var subscription =
                await _subscriptionService.GetCurrentAsync(
                    cancellationToken);

            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(
                await _entitlements.GetOverviewAsync(
                    cancellationToken));
        }
        catch (SubscriptionAccessException ex)
        {
            return StatusCode(
                ex.StatusCode,
                new
                {
                    code = ex.Code,
                    message = ex.Message
                });
        }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _payments.ListAsync(
                cancellationToken));
    }

    [HttpPost("change-plan")]
    public async Task<IActionResult> ChangePlan(
        InitializeSubscriptionPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(
                await _payments.InitializeAsync(
                    request,
                    cancellationToken));
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

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(
                await _payments.CancelCurrentAsync(
                    cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("manage-link")]
    public async Task<IActionResult> GetManageLink(
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                await _payments.GetManageLinkAsync(
                    cancellationToken);

            return Ok(
                new SubscriptionManageLinkResponse
                {
                    Url = url
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
}
