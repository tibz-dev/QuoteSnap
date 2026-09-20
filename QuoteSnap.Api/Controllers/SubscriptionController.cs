using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Infrastructure.Subscriptions;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionController(
        SubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
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
}