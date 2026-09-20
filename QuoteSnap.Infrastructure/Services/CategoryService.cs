using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Categories;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class CategoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public CategoryService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var businessId = GetBusinessId();

        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var businessId = GetBusinessId();

        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BusinessId == businessId)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request)
    {
        var businessId = GetBusinessId();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Category name is required.");
        }

        var name = request.Name.Trim();

        var exists = await _dbContext.Categories
            .AnyAsync(x =>
                x.BusinessId == businessId &&
                x.Name == name);

        if (exists)
        {
            throw new InvalidOperationException(
                "A category with this name already exists.");
        }

        var category = new Category
        {
            BusinessId = businessId,
            Name = name,
            Description = Clean(request.Description),
            IsActive = true
        };

        _dbContext.Categories.Add(category);

        await _dbContext.SaveChangesAsync();

        return Map(category);
    }

    public async Task<CategoryDto?> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request)
    {
        var businessId = GetBusinessId();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Category name is required.");
        }

        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (category is null)
        {
            return null;
        }

        var name = request.Name.Trim();

        var duplicate = await _dbContext.Categories
            .AnyAsync(x =>
                x.BusinessId == businessId &&
                x.Id != id &&
                x.Name == name);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A category with this name already exists.");
        }

        category.Name = name;
        category.Description = Clean(request.Description);
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Map(category);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var businessId = GetBusinessId();

        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (category is null)
        {
            return false;
        }

        var hasItems = await _dbContext.CatalogueItems
            .AnyAsync(x =>
                x.BusinessId == businessId &&
                x.CategoryId == id);

        if (hasItems)
        {
            throw new InvalidOperationException(
                "This category cannot be deleted because it contains catalogue items.");
        }

        _dbContext.Categories.Remove(category);

        await _dbContext.SaveChangesAsync();

        return true;
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

    private static CategoryDto Map(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }
}