using QuoteSnap.Domain.Common;
using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Domain.Entities;

public class CatalogueItem : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ItemType Type { get; set; }

    public PricingType PricingType { get; set; }

    public decimal? DefaultPrice { get; set; }

    public string Unit { get; set; } = "Unit";

    public bool IsTaxable { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public Business Business { get; set; } = null!;
}