using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Email;
using QuoteSnap.Infrastructure.Email;
using QuoteSnap.Infrastructure.Services;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/email-connections")]
public class EmailConnectionsController : ControllerBase
{
    private readonly EmailConnectionService _emailConnectionService;
    private readonly GoogleOAuthService _googleOAuthService;
    private readonly GoogleOAuthStateService _stateService;
    private readonly ICurrentUserService _currentUser;

    public EmailConnectionsController(
        EmailConnectionService emailConnectionService,
        GoogleOAuthService googleOAuthService,
        GoogleOAuthStateService stateService,
        ICurrentUserService currentUser)
    {
        _emailConnectionService =
            emailConnectionService;

        _googleOAuthService =
            googleOAuthService;

        _stateService =
            stateService;

        _currentUser =
            currentUser;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<EmailConnectionDto>>>
        GetAll()
    {
        return Ok(
            await _emailConnectionService
                .GetAllAsync());
    }

    [Authorize]
    [HttpGet("google/connect")]
    public IActionResult ConnectGoogle()
    {
        if (_currentUser.BusinessId == Guid.Empty ||
            _currentUser.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var state =
            _stateService.Create(
                _currentUser.BusinessId,
                _currentUser.UserId);

        var authorizationUrl =
            _googleOAuthService
                .GetAuthorizationUrl(state);

        return Ok(new
        {
            authorizationUrl
        });
    }

    [AllowAnonymous]
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(new
            {
                message =
                    "Google authorization failed.",
                error
            });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new
            {
                message =
                    "Google authorization code is missing."
            });
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(new
            {
                message =
                    "Google OAuth state is missing."
            });
        }

        try
        {
            var oauthState =
                _stateService.Read(state);

            await _googleOAuthService
                .ConnectAsync(
                    code,
                    oauthState.BusinessId,
                    cancellationToken);

            return Ok(new
            {
                message =
                    "Google account connected successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpDelete("google")]
    public async Task<IActionResult>
        DisconnectGoogle()
    {
        await _emailConnectionService
            .DisconnectGoogleAsync();

        return NoContent();
    }
}