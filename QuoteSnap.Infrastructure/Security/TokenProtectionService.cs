using Microsoft.AspNetCore.DataProtection;
using QuoteSnap.Application.Security;

namespace QuoteSnap.Infrastructure.Security;

public class TokenProtectionService
    : ITokenProtectionService
{
    private const string Purpose =
        "QuoteSnap.EmailConnections.RefreshToken.v1";

    private readonly IDataProtector _protector;

    public TokenProtectionService(
        IDataProtectionProvider dataProtectionProvider)
    {
        _protector =
            dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Token cannot be empty.",
                nameof(value));
        }

        return _protector.Protect(value);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            throw new ArgumentException(
                "Protected token cannot be empty.",
                nameof(protectedValue));
        }

        return _protector.Unprotect(protectedValue);
    }
}