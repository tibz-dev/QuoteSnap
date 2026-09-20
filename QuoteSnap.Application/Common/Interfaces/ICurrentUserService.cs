namespace QuoteSnap.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }

    Guid BusinessId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }
}