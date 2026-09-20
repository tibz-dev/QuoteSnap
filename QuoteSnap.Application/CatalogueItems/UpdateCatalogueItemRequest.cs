using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.CatalogueItems;

public class UpdateCatalogueItemRequest
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
}