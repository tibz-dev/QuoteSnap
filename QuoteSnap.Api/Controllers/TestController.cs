using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Common.Interfaces;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/test")]
[Authorize]
public class TestController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public TestController(
        ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public IActionResult GetMe()
    {
        return Ok(new
        {
            userId = _currentUser.UserId,
            businessId = _currentUser.BusinessId,
            email = _currentUser.Email,
            isAuthenticated = _currentUser.IsAuthenticated
        });
    }
}