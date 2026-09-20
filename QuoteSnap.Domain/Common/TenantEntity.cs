namespace QuoteSnap.Domain.Common;

public abstract class TenantEntity : BaseEntity
{
    public Guid BusinessId { get; set; }
}