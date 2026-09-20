using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuoteSnap.Domain.Entities;

namespace QuoteSnap.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration
    : IEntityTypeConfiguration<Subscription>
{
    public void Configure(
        EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentProvider)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalCustomerId)
            .HasMaxLength(500);

        builder.Property(x => x.ExternalSubscriptionId)
            .HasMaxLength(500);

        builder.HasIndex(x => x.BusinessId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.Status,
            x.TrialEndsAt
        });

        builder.HasIndex(x => new
        {
            x.Status,
            x.CurrentPeriodEndsAt
        });

        builder.HasOne(x => x.Business)
            .WithOne(x => x.Subscription)
            .HasForeignKey<Subscription>(
                x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}