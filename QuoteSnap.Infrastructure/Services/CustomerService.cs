using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Customers;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Services;

public class CustomerService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public CustomerService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var businessId = GetBusinessId();

        return await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.Name)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Email = x.Email,
                Phone = x.Phone,
                Address = x.Address,
                TaxRegistrationNumber = x.TaxRegistrationNumber,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var businessId = GetBusinessId();

        return await _dbContext.Customers
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BusinessId == businessId)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Email = x.Email,
                Phone = x.Phone,
                Address = x.Address,
                TaxRegistrationNumber = x.TaxRegistrationNumber,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CustomerDto> CreateAsync(
        CreateCustomerRequest request)
    {
        var businessId = GetBusinessId();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Customer name is required.");
        }

        var customer = new Customer
        {
            BusinessId = businessId,
            Name = request.Name.Trim(),
            CompanyName = Clean(request.CompanyName),
            Email = Clean(request.Email),
            Phone = Clean(request.Phone),
            Address = Clean(request.Address),
            TaxRegistrationNumber =
                Clean(request.TaxRegistrationNumber)
        };

        _dbContext.Customers.Add(customer);

        await _dbContext.SaveChangesAsync();

        return Map(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request)
    {
        var businessId = GetBusinessId();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Customer name is required.");
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (customer is null)
        {
            return null;
        }

        customer.Name = request.Name.Trim();
        customer.CompanyName = Clean(request.CompanyName);
        customer.Email = Clean(request.Email);
        customer.Phone = Clean(request.Phone);
        customer.Address = Clean(request.Address);
        customer.TaxRegistrationNumber =
            Clean(request.TaxRegistrationNumber);

        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Map(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var businessId = GetBusinessId();

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.BusinessId == businessId);

        if (customer is null)
        {
            return false;
        }

        _dbContext.Customers.Remove(customer);

        await _dbContext.SaveChangesAsync();

        return true;
    }

    private Guid GetBusinessId()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        if (_currentUser.BusinessId == Guid.Empty)
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

    private static CustomerDto Map(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            CompanyName = customer.CompanyName,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            TaxRegistrationNumber =
                customer.TaxRegistrationNumber,
            CreatedAt = customer.CreatedAt
        };
    }
}