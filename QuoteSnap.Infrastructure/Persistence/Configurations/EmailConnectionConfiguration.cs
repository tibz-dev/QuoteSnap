using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuoteSnap.Domain.Entities;

namespace QuoteSnap.Infrastructure.Persistence.Configurations;

public class EmailConnectionConfiguration
    : IEntityTypeConfiguration<EmailConnection>
{
    public void Configure(
        EntityTypeBuilder<EmailConnection> builder)
    {
        builder.ToTable("EmailConnections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmailAddress)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.EncryptedRefreshToken)
            .IsRequired();

        builder.HasIndex(x => x.BusinessId);

        builder.HasIndex(x => new
        {
            x.BusinessId,
            x.Provider
        })
        .IsUnique();

        builder.HasOne(x => x.Business)
            .WithMany(x => x.EmailConnections)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}