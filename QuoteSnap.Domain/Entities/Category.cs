using QuoteSnap.Domain.Common;

namespace QuoteSnap.Domain.Entities;

public class Category : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public Business Business { get; set; } = null!;

    public ICollection<CatalogueItem> Items { get; set; }
        = new List<CatalogueItem>();
}