using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuoteSnap.Domain.Entities;
using QuoteSnap.Infrastructure.Identity;

namespace QuoteSnap.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CatalogueItem> CatalogueItems => Set<CatalogueItem>();

    public DbSet<Quote> Quotes => Set<Quote>();

    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<DocumentDelivery> DocumentDeliveries =>Set<DocumentDelivery>();

    public DbSet<Subscription> Subscriptions =>Set<Subscription>();

    public DbSet<EmailConnection> EmailConnections => Set<EmailConnection>();

    public DbSet<Notification> Notifications =>
    Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}