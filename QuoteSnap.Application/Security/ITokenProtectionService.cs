namespace QuoteSnap.Application.Security;

public interface ITokenProtectionService
{
    string Protect(string value);

    string Unprotect(string protectedValue);
}