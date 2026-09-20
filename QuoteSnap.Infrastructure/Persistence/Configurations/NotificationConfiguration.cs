using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuoteSnap.Domain.Entities;

namespace QuoteSnap.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(
        EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Recipient)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.HtmlBody)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(100);

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(500);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(100);

        builder.HasIndex(x => x.BusinessId);

        builder.HasIndex(x => new
        {
            x.Status,
            x.ScheduledFor
        });

        builder.HasIndex(x => new
        {
            x.BusinessId,
            x.Type
        });

        builder.HasIndex(x => new
        {
            x.ReferenceType,
            x.ReferenceId
        });

        builder.HasOne(x => x.Business)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}