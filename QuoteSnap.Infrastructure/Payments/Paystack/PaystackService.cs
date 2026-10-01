using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace QuoteSnap.Infrastructure.Payments.Paystack;

public class PaystackService
{
    private readonly HttpClient _httpClient;
    private readonly PaystackSettings _settings;

    public PaystackService(
        HttpClient httpClient,
        IOptions<PaystackSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<PaystackInitializeResult> InitializeTransactionAsync(
        string email,
        decimal amount,
        string currency,
        string reference,
        string planCode,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        // Paystack expects the amount in the currency's
        // smallest denomination. For ZAR: R99.00 -> 9900 cents.
        var amountInMinorUnits =
            decimal.ToInt64(
                decimal.Round(
                    amount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "transaction/initialize");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _settings.SecretKey);

        request.Content =
            JsonContent.Create(new
            {
                email,
                amount = amountInMinorUnits,
                currency,
                reference,
                plan = planCode,
                callback_url = _settings.CallbackUrl
            });

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        var content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Paystack initialization failed: {content}");
        }

        var result =
            JsonSerializer.Deserialize<PaystackInitializeResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result is null ||
            !result.Status ||
            result.Data is null ||
            string.IsNullOrWhiteSpace(
                result.Data.AuthorizationUrl))
        {
            throw new InvalidOperationException(
                result?.Message ??
                "Paystack could not initialize the transaction.");
        }

        return new PaystackInitializeResult
        {
            AuthorizationUrl =
                result.Data.AuthorizationUrl,

            AccessCode =
                result.Data.AccessCode,

            Reference =
                result.Data.Reference
        };
    }

    public async Task<PaystackVerifyResult> VerifyTransactionAsync(
    string reference,
    CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"transaction/verify/{Uri.EscapeDataString(reference)}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _settings.SecretKey);

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        var content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Paystack verification failed: {content}");
        }

        var result =
            JsonSerializer.Deserialize<PaystackVerifyResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result is null ||
            !result.Status ||
            result.Data is null)
        {
            throw new InvalidOperationException(
                result?.Message ??
                "Paystack transaction verification failed.");
        }

        return new PaystackVerifyResult
        {
            Id = result.Data.Id,
            Status = result.Data.Status,
            Reference = result.Data.Reference,
            Amount = result.Data.Amount,
            Currency = result.Data.Currency,
            PaidAt = result.Data.PaidAt
        };
    }

    private sealed class PaystackVerifyResponse
    {
        public bool Status { get; set; }

        public string? Message { get; set; }

        public PaystackVerifyData? Data { get; set; }
    }

    private sealed class PaystackVerifyData
    {
        public ulong Id { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public long Amount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public DateTime? PaidAt { get; set; }
    }

    public async Task DisableSubscriptionAsync(
        string subscriptionCode,
        string emailToken,
        CancellationToken cancellationToken = default)
    {
        ValidateSecretKey();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "subscription/disable");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _settings.SecretKey);

        request.Content =
            JsonContent.Create(new
            {
                code = subscriptionCode,
                token = emailToken
            });

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        var content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Paystack subscription cancellation failed: {content}");
        }

        var result =
            JsonSerializer.Deserialize<PaystackActionResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result is null || !result.Status)
        {
            throw new InvalidOperationException(
                result?.Message ??
                "Paystack could not cancel the subscription.");
        }
    }

    public async Task<string> GetSubscriptionManageLinkAsync(
        string subscriptionCode,
        CancellationToken cancellationToken = default)
    {
        ValidateSecretKey();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"subscription/{Uri.EscapeDataString(subscriptionCode)}/manage/link");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _settings.SecretKey);

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        var content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Paystack subscription management failed: {content}");
        }

        var result =
            JsonSerializer.Deserialize<PaystackManageLinkResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result is null ||
            !result.Status ||
            result.Data is null ||
            string.IsNullOrWhiteSpace(result.Data.Link))
        {
            throw new InvalidOperationException(
                result?.Message ??
                "Paystack could not create a subscription management link.");
        }

        return result.Data.Link;
    }

    private void ValidateSettings()
    {
        ValidateSecretKey();

        if (string.IsNullOrWhiteSpace(
                _settings.CallbackUrl))
        {
            throw new InvalidOperationException(
                "Paystack CallbackUrl is missing.");
        }
    }

    private void ValidateSecretKey()
    {
        if (string.IsNullOrWhiteSpace(
                _settings.SecretKey))
        {
            throw new InvalidOperationException(
                "Paystack SecretKey is missing.");
        }
    }

    private sealed class PaystackActionResponse
    {
        public bool Status { get; set; }

        public string? Message { get; set; }
    }

    private sealed class PaystackManageLinkResponse
    {
        public bool Status { get; set; }

        public string? Message { get; set; }

        public PaystackManageLinkData? Data { get; set; }
    }

    private sealed class PaystackManageLinkData
    {
        public string Link { get; set; } = string.Empty;
    }

    private sealed class PaystackInitializeResponse
    {
        public bool Status { get; set; }

        public string? Message { get; set; }

        public PaystackInitializeData? Data { get; set; }
    }

    private sealed class PaystackInitializeData
    {
        [JsonPropertyName("authorization_url")]
        public string AuthorizationUrl { get; set; }
            = string.Empty;

        [JsonPropertyName("access_code")]
        public string AccessCode { get; set; }
            = string.Empty;

        [JsonPropertyName("reference")]
        public string Reference { get; set; }
            = string.Empty;
    }
}

public class PaystackInitializeResult
{
    public string AuthorizationUrl { get; set; }
        = string.Empty;

    public string AccessCode { get; set; }
        = string.Empty;

    public string Reference { get; set; }
        = string.Empty;
}

public class PaystackVerifyResult
{
    public ulong Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public long Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
}