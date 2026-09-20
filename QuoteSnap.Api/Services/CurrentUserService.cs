using QuoteSnap.Application.Common.Interfaces;
using System.Security.Claims;

namespace QuoteSnap.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var value =
                User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userId)
                ? userId
                : Guid.Empty;
        }
    }

    public Guid BusinessId
    {
        get
        {
            var value =
                User?.FindFirstValue("businessId");

            return Guid.TryParse(value, out var businessId)
                ? businessId
                : Guid.Empty;
        }
    }

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email);
}