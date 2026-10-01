using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Businesses;
using QuoteSnap.Infrastructure.Services;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/business")]
[Authorize]
public class BusinessController : ControllerBase
{
    private readonly BusinessSettingsService _service;

    public BusinessController(
        BusinessSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<BusinessSettingsDto>>
        GetCurrent()
    {
        try
        {
            return Ok(await _service.GetCurrentAsync());
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut]
    public async Task<ActionResult<BusinessSettingsDto>>
        Update(
            UpdateBusinessSettingsRequest request)
    {
        try
        {
            return Ok(await _service.UpdateAsync(request));
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
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }
}
