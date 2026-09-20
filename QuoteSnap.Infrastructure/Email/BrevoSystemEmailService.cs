using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using QuoteSnap.Application.Email;

namespace QuoteSnap.Infrastructure.Email;

public class BrevoSystemEmailService : ISystemEmailService
{
    private readonly HttpClient _httpClient;
    private readonly SystemEmailSettings _settings;

    public BrevoSystemEmailService(
        HttpClient httpClient,
        IOptions<SystemEmailSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        var requestBody = new
        {
            sender = new
            {
                name = _settings.SenderName,
                email = _settings.SenderEmail
            },

            to = new[]
            {
                new
                {
                    email = message.To
                }
            },

            subject = message.Subject,

            htmlContent = message.HtmlBody,

            replyTo = string.IsNullOrWhiteSpace(
                _settings.ReplyToEmail)
                    ? null
                    : new
                    {
                        email = _settings.ReplyToEmail
                    }
        };

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email");

        request.Headers.Add(
            "api-key",
            _settings.ApiKey);

        request.Content =
            JsonContent.Create(requestBody);

        try
        {
            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage =
                        $"System email failed. " +
                        $"HTTP {(int)response.StatusCode}: " +
                        responseBody
                };
            }

            var result =
                await response.Content
                    .ReadFromJsonAsync<BrevoSendResponse>(
                        cancellationToken: cancellationToken);

            return new EmailSendResult
            {
                Success = true,
                ProviderMessageId = result?.MessageId
            };
        }
        catch (Exception ex)
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "System email API key is missing.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "System email sender address is missing.");
        }
    }

    private sealed class BrevoSendResponse
    {
        public string? MessageId { get; set; }
    }
}