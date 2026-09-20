using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuoteSnap.Domain.Entities;

namespace QuoteSnap.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration
    : IEntityTypeConfiguration<Receipt>
{
    public void Configure(
        EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.BusinessId);

        builder.HasIndex(x => new
        {
            x.BusinessId,
            x.ReceiptNumber
        })
        .IsUnique();

        builder.HasIndex(x => x.PaymentId)
            .IsUnique();

        builder.HasOne(x => x.Payment)
            .WithOne(x => x.Receipt)
            .HasForeignKey<Receipt>(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.Receipts)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}