using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Notifications;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetRecent(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.BusinessId == Guid.Empty)
        {
            return Unauthorized();
        }

        limit = Math.Clamp(limit, 1, 50);

        var businessId = _currentUser.BusinessId;
        var userId = _currentUser.UserId;

        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .Where(x =>
                x.BusinessId == businessId &&
                (!x.UserId.HasValue ||
                 userId == Guid.Empty ||
                 x.UserId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Type = (int)x.Type,
                Status = (int)x.Status,
                Recipient = x.Recipient,
                Subject = x.Subject,
                ScheduledFor = x.ScheduledFor,
                SentAt = x.SentAt,
                FailedAt = x.FailedAt,
                ErrorMessage = x.ErrorMessage,
                ReferenceType = x.ReferenceType,
                ReferenceId = x.ReferenceId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }
}
