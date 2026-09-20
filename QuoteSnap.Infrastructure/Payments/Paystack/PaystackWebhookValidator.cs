using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace QuoteSnap.Infrastructure.Payments.Paystack;

public class PaystackWebhookValidator
{
    private readonly PaystackSettings _settings;

    public PaystackWebhookValidator(
        IOptions<PaystackSettings> options)
    {
        _settings = options.Value;
    }

    public bool IsValid(
        string payload,
        string signature)
    {
        if (string.IsNullOrWhiteSpace(
                _settings.SecretKey) ||
            string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var key =
            Encoding.UTF8.GetBytes(
                _settings.SecretKey);

        var data =
            Encoding.UTF8.GetBytes(payload);

        using var hmac =
            new HMACSHA512(key);

        var hash =
            hmac.ComputeHash(data);

        var expected =
            Convert.ToHexString(hash)
                .ToLowerInvariant();

        return CryptographicOperations
            .FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected),
                Encoding.ASCII.GetBytes(
                    signature.ToLowerInvariant()));
    }
}