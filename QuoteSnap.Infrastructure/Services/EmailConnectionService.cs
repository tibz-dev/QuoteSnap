using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Email;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class EmailConnectionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public EmailConnectionService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<EmailConnectionDto>> GetAllAsync()
    {
        var businessId = GetBusinessId();

        return await _dbContext.EmailConnections
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.Provider)
            .Select(x => new EmailConnectionDto
            {
                Id = x.Id,
                Provider = x.Provider,
                EmailAddress = x.EmailAddress,
                IsActive = x.IsActive,
                ConnectedAt = x.ConnectedAt,
                LastUsedAt = x.LastUsedAt
            })
            .ToListAsync();
    }

    public async Task DisconnectGoogleAsync()
    {
        var businessId = GetBusinessId();

        var connection =
            await _dbContext.EmailConnections
                .FirstOrDefaultAsync(
                    x =>
                        x.BusinessId == businessId &&
                        x.Provider == EmailProvider.Google);

        if (connection is null)
            return;

        _dbContext.EmailConnections.Remove(
            connection);

        await _dbContext.SaveChangesAsync();
    }

    private Guid GetBusinessId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.BusinessId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Business information is missing.");
        }

        return _currentUser.BusinessId;
    }
}