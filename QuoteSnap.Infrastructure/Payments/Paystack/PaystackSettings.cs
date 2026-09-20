namespace QuoteSnap.Infrastructure.Payments.Paystack;

public class PaystackSettings
{
    public const string SectionName = "Paystack";

    public string SecretKey { get; set; } = string.Empty;

    public string PublicKey { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = string.Empty;

    public string ProMonthlyPlanCode { get; set; } = string.Empty;

    public string BusinessMonthlyPlanCode { get; set; } = string.Empty;
}