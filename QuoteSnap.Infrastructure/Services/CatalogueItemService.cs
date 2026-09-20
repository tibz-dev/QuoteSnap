using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.CatalogueItems;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class CatalogueItemService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public CatalogueItemService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<CatalogueItemDto>> GetAllAsync()
    {
        var businessId = GetBusinessId();

        return await _dbContext.CatalogueItems
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogueItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Type = x.Type,
                PricingType = x.PricingType,
                DefaultPrice = x.DefaultPrice,
                Unit = x.Unit,
                IsTaxable = x.IsTaxable,
                IsActive = x.IsActive,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null
                    ? x.Category.Name
                    : null,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CatalogueItemDto?> GetByIdAsync(Guid id)
    {
        var businessId = GetBusinessId();

        return await _dbContext.CatalogueItems
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BusinessId == businessId)
            .Select(x => new CatalogueItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Type = x.Type,
                PricingType = x.PricingType,
                DefaultPrice = x.DefaultPrice,
                Unit = x.Unit,
                IsTaxable = x.IsTaxable,
                IsActive = x.IsActive,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null
                    ? x.Category.Name
                    : null,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CatalogueItemDto> CreateAsync(
        CreateCatalogueItemRequest request)
    {
        var businessId = GetBusinessId();

        ValidateRequest(
            request.Name,
            request.Unit,
            request.DefaultPrice);

        await ValidateCategoryAsync(
            businessId,
            request.CategoryId);

        var item = new CatalogueItem
        {
            BusinessId = businessId,
            Name = request.Name.Trim(),
            Description = Clean(request.Description),
            Type = request.Type,
            PricingType = request.PricingType,
            DefaultPrice = request.DefaultPrice,
            Unit = request.Unit.Trim(),
            IsTaxable = request.IsTaxable,
            IsActive = true,
            CategoryId = request.CategoryId
        };

        _dbContext.CatalogueItems.Add(item);

        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(item.Id)
            ?? throw new InvalidOperationException(
                "Catalogue item could not be loaded.");
    }

    public async Task<CatalogueItemDto?> UpdateAsync(
        Guid id,
        UpdateCatalogueItemRequest request)
    {
        var businessId = GetBusinessId();

        ValidateRequest(
            request.Name,
            request.Unit,
            request.DefaultPrice);

        var item = await _dbContext.CatalogueItems
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (item is null)
        {
            return null;
        }

        await ValidateCategoryAsync(
            businessId,
            request.CategoryId);

        item.Name = request.Name.Trim();
        item.Description = Clean(request.Description);
        item.Type = request.Type;
        item.PricingType = request.PricingType;
        item.DefaultPrice = request.DefaultPrice;
        item.Unit = request.Unit.Trim();
        item.IsTaxable = request.IsTaxable;
        item.IsActive = request.IsActive;
        item.CategoryId = request.CategoryId;
        item.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(item.Id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var businessId = GetBusinessId();

        var item = await _dbContext.CatalogueItems
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (item is null)
        {
            return false;
        }

        _dbContext.CatalogueItems.Remove(item);

        await _dbContext.SaveChangesAsync();

        return true;
    }

    private async Task ValidateCategoryAsync(
        Guid businessId,
        Guid? categoryId)
    {
        if (!categoryId.HasValue)
        {
            return;
        }

        var categoryExists = await _dbContext.Categories
            .AnyAsync(x =>
                x.Id == categoryId.Value &&
                x.BusinessId == businessId);

        if (!categoryExists)
        {
            throw new ArgumentException(
                "The selected category does not exist.");
        }
    }

    private static void ValidateRequest(
        string name,
        string unit,
        decimal? defaultPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Catalogue item name is required.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException(
                "Unit is required.");
        }

        if (defaultPrice.HasValue &&
            defaultPrice.Value < 0)
        {
            throw new ArgumentException(
                "Default price cannot be negative.");
        }
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

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}