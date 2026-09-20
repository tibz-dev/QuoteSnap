using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuoteSnap.Domain.Entities;

namespace QuoteSnap.Infrastructure.Persistence.Configurations;

public class DocumentDeliveryConfiguration
    : IEntityTypeConfiguration<DocumentDelivery>
{
    public void Configure(
        EntityTypeBuilder<DocumentDelivery> builder)
    {
        builder.ToTable("DocumentDeliveries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Recipient)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.Provider)
            .HasMaxLength(50);

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(500);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.BusinessId);

        builder.HasIndex(x => new
        {
            x.BusinessId,
            x.DocumentType,
            x.DocumentId
        });

        builder.HasOne(x => x.Business)
            .WithMany(x => x.DocumentDeliveries)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}