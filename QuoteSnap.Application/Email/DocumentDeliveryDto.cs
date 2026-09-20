using QuoteSnap.Domain.Enums;

namespace QuoteSnap.Application.Email;

public class DocumentDeliveryDto
{
    public Guid Id { get; set; }

    public DocumentType DocumentType { get; set; }

    public Guid DocumentId { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    public DeliveryChannel Channel { get; set; }

    public string Recipient { get; set; } = string.Empty;

    public DeliveryStatus Status { get; set; }

    public string? Provider { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }
}