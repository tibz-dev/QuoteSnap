using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.CatalogueItems;

public class CatalogueItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ItemType Type { get; set; }

    public PricingType PricingType { get; set; }

    public decimal? DefaultPrice { get; set; }

    public string Unit { get; set; } = string.Empty;

    public bool IsTaxable { get; set; }

    public bool IsActive { get; set; }

    public Guid? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public DateTime CreatedAt { get; set; }
}