using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuoteSnap.Application.Authentication;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Domain.Enums;
using QuoteSnap.Infrastructure.Identity;
using QuoteSnap.Infrastructure.Persistence;

namespace QuoteSnap.Infrastructure.Authentication;

public class AuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "An account with this email already exists.");
        }

        var business = new Business
        {
            Name = request.BusinessName.Trim(),

            CountryCode = request.CountryCode
                .Trim()
                .ToUpperInvariant(),

            CurrencyCode = request.CurrencyCode
                .Trim()
                .ToUpperInvariant(),

            SubscriptionPlan = SubscriptionPlan.Free,

            QuotePrefix = "QT",

            NextQuoteNumber = 1,

            InvoicePrefix = "INV",

            NextInvoiceNumber = 1,

            DefaultQuoteValidityDays = 7
        };

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,

            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),

            BusinessId = business.Id,

            IsActive = true
        };

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            _dbContext.Businesses.Add(business);

            await _dbContext.SaveChangesAsync();

            var userResult =
                await _userManager.CreateAsync(
                    user,
                    request.Password);

            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        " ",
                        userResult.Errors.Select(
                            x => x.Description)));
            }

            const string ownerRole = "Owner";

            if (!await _roleManager.RoleExistsAsync(ownerRole))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole<Guid>(ownerRole));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            " ",
                            roleResult.Errors.Select(
                                x => x.Description)));
                }
            }

            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    ownerRole);

            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        " ",
                        addRoleResult.Errors.Select(
                            x => x.Description)));
            }

            await transaction.CommitAsync();

            return await _jwtTokenService
                .CreateTokenAsync(user);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "This account is inactive.");
        }

        var validPassword =
            await _userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!validPassword)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        return await _jwtTokenService
            .CreateTokenAsync(user);
    }
}