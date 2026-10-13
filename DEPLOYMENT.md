# QuoteSnap deployment checklist

QuoteSnap is a split deployment:

- `QuoteSnap.Web`: Vite/React static frontend.
- `QuoteSnap.Api`: ASP.NET Core 8 API.
- SQL Server: production database.
- Paystack: subscription checkout and recurring billing.
- Google OAuth/Gmail: optional invoice delivery connection.
- Brevo: system emails such as verification and password reset.

## 1. Frontend

For Vercel, set the project root directory to:

```text
QuoteSnap.Web
```

The repository includes `QuoteSnap.Web/vercel.json`.

Set:

```text
VITE_API_BASE_URL=https://YOUR_API_HOST
```

Do not add a trailing slash.

## 2. API container

The repository root contains a production `Dockerfile`.

The container listens on:

```text
8080
```

Health endpoint:

```text
GET /health
```

Use `/health` as the host platform health check.

## 3. Required API environment variables

Use double underscores for nested ASP.NET Core configuration keys.

### Database

```text
ConnectionStrings__DefaultConnection=YOUR_PRODUCTION_SQL_SERVER_CONNECTION_STRING
```

### Frontend / CORS / callback destination

```text
App__FrontendBaseUrl=https://YOUR_FRONTEND_HOST
```

### JWT

```text
Jwt__Key=GENERATE_A_LONG_RANDOM_SECRET
Jwt__Issuer=QuoteSnap.Api
Jwt__Audience=QuoteSnap.Web
Jwt__ExpiryMinutes=60
```

### Paystack

```text
Paystack__SecretKey=YOUR_PAYSTACK_SECRET_KEY
Paystack__PublicKey=YOUR_PAYSTACK_PUBLIC_KEY
Paystack__CallbackUrl=https://YOUR_API_HOST/api/subscription-payments/callback
Paystack__ProMonthlyPlanCode=YOUR_PAYSTACK_PRO_PLAN_CODE
Paystack__BusinessMonthlyPlanCode=YOUR_PAYSTACK_BUSINESS_PLAN_CODE
```

Configure the Paystack webhook URL as:

```text
https://YOUR_API_HOST/api/subscription-payments/webhook
```

### Google OAuth / Gmail

```text
Gmail__ClientId=YOUR_GOOGLE_CLIENT_ID
Gmail__ClientSecret=YOUR_GOOGLE_CLIENT_SECRET
Gmail__RedirectUri=https://YOUR_API_HOST/api/email-connections/google/callback
```

Register the exact same redirect URI in the Google Cloud OAuth client.

### System email / Brevo

```text
SystemEmail__ApiKey=YOUR_BREVO_API_KEY
SystemEmail__SenderEmail=YOUR_VERIFIED_SENDER
SystemEmail__SenderName=QuoteSnap
SystemEmail__ReplyToEmail=YOUR_REPLY_TO_EMAIL
```

### Persistent ASP.NET Data Protection keys

Google refresh tokens are protected with ASP.NET Data Protection before storage.
For container hosting, mount a persistent volume and configure:

```text
DataProtection__KeysPath=/var/quotesnap/keys
```

The directory must survive container redeployments. Losing these keys can make already-stored protected Gmail refresh tokens unreadable.

## 4. Subscription configuration

Defaults are committed in `appsettings.json`, but production can override any of them:

```text
Subscriptions__TrialDays=14
Subscriptions__ProMonthlyPrice=99
Subscriptions__BusinessMonthlyPrice=199
Subscriptions__BillingCurrency=ZAR
Subscriptions__FreeCustomerLimit=5
Subscriptions__FreeCatalogueItemLimit=10
Subscriptions__FreeMonthlyQuoteLimit=5
Subscriptions__FreeMonthlyInvoiceLimit=5
Subscriptions__FreeMonthlyReceiptLimit=10
Subscriptions__ProCustomerLimit=100
Subscriptions__ProCatalogueItemLimit=250
Subscriptions__ProMonthlyQuoteLimit=100
Subscriptions__ProMonthlyInvoiceLimit=100
Subscriptions__ProMonthlyReceiptLimit=250
Subscriptions__TrialUsesProAccess=true
```

## 5. Database migrations

Do not rely on the web process to migrate the production database automatically.

Before the first production start, apply migrations from a trusted deployment shell using the production connection string.

Example:

```powershell
$env:ConnectionStrings__DefaultConnection="YOUR_PRODUCTION_CONNECTION_STRING"
dotnet ef database update --project QuoteSnap.Infrastructure --startup-project QuoteSnap.Api
```

Repeat migration deployment whenever a new EF migration is added.

## 6. Production smoke test

After deployment, test in this order:

1. `GET https://YOUR_API_HOST/health`
2. Register a new business.
3. Sign in and reload the browser.
4. Create and archive a customer.
5. Create a catalogue item.
6. Create and accept a quote.
7. Convert it to an invoice.
8. Record payment and generate a receipt.
9. Download quote, invoice and receipt PDFs.
10. Connect Google and send an invoice email.
11. Open Subscription and verify trial usage.
12. Complete a Paystack test checkout.
13. Confirm the Paystack webhook updates the subscription and billing history.
14. Test cancel-renewal and billing-management flows.
15. Test password reset email and reset link.

## 7. Production security notes

- Never commit JWT, Paystack, Google, Brevo, or database secrets.
- Use live Paystack keys only after the full test-mode flow passes.
- Rotate any bearer token or secret that is accidentally pasted into logs or chat.
- Keep Swagger development-only.
- Use HTTPS for both frontend and API.
