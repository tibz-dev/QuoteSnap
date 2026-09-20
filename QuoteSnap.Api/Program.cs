using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuoteSnap.Api.Services;
using QuoteSnap.Application.Common.Interfaces;
using QuoteSnap.Application.Email;
using QuoteSnap.Application.Security;
using QuoteSnap.Infrastructure.Authentication;
using QuoteSnap.Infrastructure.BackgroundJobs;
using QuoteSnap.Infrastructure.Email;
using QuoteSnap.Infrastructure.Identity;
using QuoteSnap.Infrastructure.Persistence;
using QuoteSnap.Infrastructure.Security;
using QuoteSnap.Infrastructure.Services;
using QuoteSnap.Infrastructure.Subscriptions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// SQL Server Express
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "JWT signing key is missing.");
}

builder.Services.Configure<SubscriptionSettings>(
    builder.Configuration.GetSection(
        SubscriptionSettings.SectionName));

builder.Services.AddScoped<SubscriptionService>();

builder.Services.AddHostedService<
    SubscriptionLifecycleWorker>();
// JWT Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Key)),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

// QuoteSnap services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CatalogueItemService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<ReceiptService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<NotificationService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHostedService<NotificationWorker>();

builder.Services.Configure<SystemEmailSettings>(
    builder.Configuration.GetSection(
        SystemEmailSettings.SectionName));

builder.Services.AddHttpClient<
    ISystemEmailService,
    BrevoSystemEmailService>();

builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();

builder.Services.Configure<GmailSettings>(
    builder.Configuration.GetSection(
        GmailSettings.SectionName));

builder.Services.AddScoped<IEmailService, GmailEmailService>();

builder.Services.AddScoped<InvoiceEmailService>();

builder.Services.AddScoped<GoogleOAuthService>();

builder.Services.AddDataProtection()
    .SetApplicationName("QuoteSnap");

builder.Services.AddScoped<ITokenProtectionService,TokenProtectionService>();

builder.Services.AddScoped<GoogleOAuthStateService>();

builder.Services.AddScoped<EmailConnectionService>();

builder.Services.AddScoped<GoogleOAuthService>();

builder.Services.AddScoped<GoogleOAuthStateService>();

builder.Services.AddScoped<DocumentDeliveryService>();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "QuoteSnap API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

QuestPDF.Settings.License =
    QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();