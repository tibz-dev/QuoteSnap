using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuoteSnap.Domain.Entities;

namespace QuoteSnap.Infrastructure.Persistence.Configurations;

public class SubscriptionPaymentConfiguration
    : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(
        EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("SubscriptionPayments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.PaymentProvider)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalPaymentId)
            .HasMaxLength(500);

        builder.Property(x => x.ExternalReference)
            .HasMaxLength(500);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.BusinessId);

        builder.HasIndex(x => x.SubscriptionId);

        builder.HasIndex(x => new
        {
            x.PaymentProvider,
            x.ExternalPaymentId
        });

        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Business)
            .WithMany(x => x.SubscriptionPayments)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}