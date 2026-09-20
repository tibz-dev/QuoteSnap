using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuoteSnap.Domain.Entities;

namespace QuoteSnap.Infrastructure.Persistence.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        builder.Property(x => x.Phone)
            .HasMaxLength(30);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        // Location
        builder.Property(x => x.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        // Currency
        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        // Tax
        builder.Property(x => x.TaxName)
            .HasMaxLength(50);

        builder.Property(x => x.TaxRegistrationNumber)
            .HasMaxLength(100);

        builder.Property(x => x.DefaultTaxRate)
            .HasPrecision(7, 4);

        // Banking
        builder.Property(x => x.BankName)
            .HasMaxLength(100);

        builder.Property(x => x.AccountHolder)
            .HasMaxLength(150);

        builder.Property(x => x.AccountNumber)
            .HasMaxLength(100);

        builder.Property(x => x.BranchCode)
            .HasMaxLength(50);

        // Quote settings
        builder.Property(x => x.QuotePrefix)
            .IsRequired()
            .HasMaxLength(20);

        // Invoice settings
        builder.Property(x => x.InvoicePrefix)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.SubscriptionPlan)
            .IsRequired();

        // Relationships
        builder.HasMany(x => x.Customers)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Categories)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CatalogueItems)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Quotes)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.ReceiptPrefix)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.NextReceiptNumber)
            .IsRequired();
    }
}