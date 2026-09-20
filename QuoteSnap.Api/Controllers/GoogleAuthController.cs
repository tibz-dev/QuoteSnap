using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Infrastructure.Email;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/google")]
public class GoogleAuthController : ControllerBase
{
    private readonly GoogleOAuthService _googleOAuthService;

    public GoogleAuthController(
        GoogleOAuthService googleOAuthService)
    {
        _googleOAuthService = googleOAuthService;
    }

    [Authorize]
    [HttpGet("connect")]
    public IActionResult Connect()
    {
        var redirectUri =
            $"{Request.Scheme}://{Request.Host}/api/google/callback";

        var authorizationUrl =
            _googleOAuthService.GetAuthorizationUrl(
                redirectUri);

        return Redirect(authorizationUrl);
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(new
            {
                message = "Google authorization failed.",
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

        var redirectUri =
            $"{Request.Scheme}://{Request.Host}/api/google/callback";

        var result =
            await _googleOAuthService.ExchangeCodeAsync(
                code,
                redirectUri,
                cancellationToken);

        return Ok(new
        {
            message =
                "Google account connected successfully.",

            hasRefreshToken =
                !string.IsNullOrWhiteSpace(
                    result.RefreshToken),

            refreshToken =
                result.RefreshToken
        });
    }
}