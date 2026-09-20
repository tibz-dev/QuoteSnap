using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Authentication;
using QuoteSnap.Infrastructure.Authentication;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);

            return Ok(result);
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
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(
    [FromQuery] Guid userId,
    [FromQuery] string token)
    {
        try
        {
            await _authService.VerifyEmailAsync(
                new VerifyEmailRequest
                {
                    UserId = userId,
                    Token = token
                });

            return Ok(new
            {
                message = "Email verified successfully."
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

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
    [FromBody] ResendVerificationEmailRequest request)
    {
        await _authService
            .ResendVerificationEmailAsync(request);

        return Ok(new
        {
            message =
                "If the account exists and requires verification, " +
                "a verification email has been sent."
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);

        return Ok(new
        {
            message =
                "If an account exists for that email, " +
                "a password reset email has been sent."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
    [FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);

            return Ok(new
            {
                message = "Password reset successfully."
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