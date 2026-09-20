using Microsoft.AspNetCore.Identity;

namespace QuoteSnap.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid BusinessId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}