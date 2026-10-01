import { useEffect, useMemo, useState, type FormEvent } from "react"
import {
  BadgeDollarSign,
  Building2,
  CheckCircle2,
  CreditCard,
  ExternalLink,
  Landmark,
  LoaderCircle,
  Mail,
  RefreshCw,
  Save,
  ShieldCheck,
  Unplug,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { settingsApi } from "./settings.api"
import type {
  BusinessSettings,
  BusinessSettingsInput,
  EmailConnection,
  Subscription,
} from "./settings.types"

const planLabels: Record<number, string> = {
  1: "Free",
  2: "Pro",
  3: "Business",
}

const statusLabels: Record<number, string> = {
  1: "Trial",
  2: "Active",
  3: "Past due",
  4: "Cancelled",
  5: "Expired",
}

export function SettingsPage() {
  const [business, setBusiness] = useState<BusinessSettings | null>(null)
  const [form, setForm] = useState<BusinessSettingsInput | null>(null)
  const [emailConnections, setEmailConnections] = useState<EmailConnection[]>([])
  const [subscription, setSubscription] = useState<Subscription | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [isConnectingEmail, setIsConnectingEmail] = useState(false)
  const [isDisconnectingEmail, setIsDisconnectingEmail] = useState(false)
  const [upgradingPlan, setUpgradingPlan] = useState<number | null>(null)
  const [error, setError] = useState("")
  const [success, setSuccess] = useState("")

  useEffect(() => {
    void loadSettings()
  }, [])

  async function loadSettings() {
    try {
      setIsLoading(true)
      setError("")

      const [businessData, emailData, subscriptionData] = await Promise.all([
        settingsApi.getBusiness(),
        settingsApi.getEmailConnections(),
        settingsApi.getSubscription(),
      ])

      setBusiness(businessData)
      setForm(stripId(businessData))
      setEmailConnections(emailData)
      setSubscription(subscriptionData)
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your settings.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const googleConnection = useMemo(
    () => emailConnections.find((connection) => connection.isActive) || null,
    [emailConnections],
  )

  const updateField = <K extends keyof BusinessSettingsInput>(
    field: K,
    value: BusinessSettingsInput[K],
  ) => {
    setForm((current) =>
      current ? { ...current, [field]: value } : current,
    )
  }

  const handleSave = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!form) return

    if (!form.name.trim()) {
      setError("Business name is required.")
      return
    }

    try {
      setIsSaving(true)
      setError("")
      setSuccess("")

      const updated = await settingsApi.updateBusiness({
        ...form,
        name: form.name.trim(),
        email: clean(form.email),
        phone: clean(form.phone),
        address: clean(form.address),
        countryCode: form.countryCode.trim().toUpperCase(),
        currencyCode: form.currencyCode.trim().toUpperCase(),
        taxName: form.isTaxRegistered ? clean(form.taxName) : null,
        taxRegistrationNumber: form.isTaxRegistered
          ? clean(form.taxRegistrationNumber)
          : null,
        defaultTaxRate: form.isTaxRegistered ? form.defaultTaxRate : null,
        bankName: clean(form.bankName),
        accountHolder: clean(form.accountHolder),
        accountNumber: clean(form.accountNumber),
        branchCode: clean(form.branchCode),
        quotePrefix: form.quotePrefix.trim().toUpperCase(),
        invoicePrefix: form.invoicePrefix.trim().toUpperCase(),
        receiptPrefix: form.receiptPrefix.trim().toUpperCase(),
      })

      setBusiness(updated)
      setForm(stripId(updated))
      setSuccess("Business settings saved.")
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't save your business settings.",
      )
    } finally {
      setIsSaving(false)
    }
  }

  const handleConnectGoogle = async () => {
    try {
      setIsConnectingEmail(true)
      setError("")
      setSuccess("")
      const result = await settingsApi.getGoogleConnectUrl()
      window.open(result.authorizationUrl, "_blank", "noopener,noreferrer")
      setSuccess(
        "Google authorization opened in a new tab. Complete it there, then refresh email status here.",
      )
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't start Google connection.",
      )
    } finally {
      setIsConnectingEmail(false)
    }
  }

  const refreshEmailConnections = async () => {
    try {
      setError("")
      setSuccess("")
      setEmailConnections(await settingsApi.getEmailConnections())
      setSuccess("Email connection status refreshed.")
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't refresh email connections.",
      )
    }
  }

  const handleDisconnectGoogle = async () => {
    if (!window.confirm("Disconnect the Google account from QuoteSnap?")) return

    try {
      setIsDisconnectingEmail(true)
      setError("")
      setSuccess("")
      await settingsApi.disconnectGoogle()
      setEmailConnections([])
      setSuccess("Google account disconnected.")
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't disconnect the Google account.",
      )
    } finally {
      setIsDisconnectingEmail(false)
    }
  }

  const handleUpgrade = async (plan: number) => {
    try {
      setUpgradingPlan(plan)
      setError("")
      setSuccess("")
      const result = await settingsApi.initializeSubscription(plan)
      window.location.assign(result.authorizationUrl)
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't start subscription checkout.",
      )
      setUpgradingPlan(null)
    }
  }

  if (isLoading || !form || !business) {
    return (
      <section className="flex min-h-[60vh] items-center justify-center">
        <LoaderCircle className="size-6 animate-spin text-primary" />
      </section>
    )
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1200px]">
        <div>
          <p className="text-sm font-semibold text-primary">SETTINGS</p>
          <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
            Business & account settings
          </h2>
          <p className="mt-2 text-sm text-muted-foreground">
            Manage the details QuoteSnap uses on documents, email delivery and billing.
          </p>
        </div>

        {(error || success) && (
          <div
            className="mt-6 rounded-xl px-4 py-3 text-sm"
            style={
              error
                ? {
                    color: "var(--status-danger)",
                    backgroundColor: "var(--status-danger-bg)",
                  }
                : {
                    color: "var(--status-success)",
                    backgroundColor: "var(--status-success-bg)",
                  }
            }
          >
            {error || success}
          </div>
        )}

        <form onSubmit={handleSave} className="mt-8 space-y-6">
          <SettingsCard
            icon={Building2}
            title="Business profile"
            description="Used across quotes, invoices, receipts and outgoing communication."
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                label="Business name"
                value={form.name}
                onChange={(value) => updateField("name", value)}
                required
              />
              <Field
                label="Business email"
                type="email"
                value={form.email || ""}
                onChange={(value) => updateField("email", value || null)}
              />
              <Field
                label="Phone"
                value={form.phone || ""}
                onChange={(value) => updateField("phone", value || null)}
              />
              <Field
                label="Address"
                value={form.address || ""}
                onChange={(value) => updateField("address", value || null)}
              />
              <Field
                label="Country code"
                value={form.countryCode}
                onChange={(value) => updateField("countryCode", value)}
                maxLength={2}
              />
              <Field
                label="Currency code"
                value={form.currencyCode}
                onChange={(value) => updateField("currencyCode", value)}
                maxLength={3}
              />
            </div>
          </SettingsCard>

          <SettingsCard
            icon={ShieldCheck}
            title="Tax & document defaults"
            description="Controls VAT/tax calculation and the numbering style of new documents."
          >
            <label className="mb-5 flex items-center gap-3 rounded-xl bg-muted p-4 text-sm font-medium">
              <input
                type="checkbox"
                checked={form.isTaxRegistered}
                onChange={(event) =>
                  updateField("isTaxRegistered", event.target.checked)
                }
                className="size-4 accent-[var(--primary)]"
              />
              This business is registered for VAT / tax
            </label>

            {form.isTaxRegistered && (
              <div className="mb-5 grid gap-4 sm:grid-cols-3">
                <Field
                  label="Tax name"
                  value={form.taxName || ""}
                  onChange={(value) => updateField("taxName", value || null)}
                  placeholder="VAT"
                />
                <Field
                  label="Tax registration number"
                  value={form.taxRegistrationNumber || ""}
                  onChange={(value) =>
                    updateField("taxRegistrationNumber", value || null)
                  }
                />
                <Field
                  label="Default tax rate (%)"
                  type="number"
                  value={
                    form.defaultTaxRate === null
                      ? ""
                      : String(form.defaultTaxRate)
                  }
                  onChange={(value) =>
                    updateField(
                      "defaultTaxRate",
                      value === "" ? null : Number(value),
                    )
                  }
                />
              </div>
            )}

            <div className="grid gap-4 sm:grid-cols-4">
              <Field
                label="Quote prefix"
                value={form.quotePrefix}
                onChange={(value) => updateField("quotePrefix", value)}
                maxLength={10}
              />
              <Field
                label="Invoice prefix"
                value={form.invoicePrefix}
                onChange={(value) => updateField("invoicePrefix", value)}
                maxLength={10}
              />
              <Field
                label="Receipt prefix"
                value={form.receiptPrefix}
                onChange={(value) => updateField("receiptPrefix", value)}
                maxLength={10}
              />
              <Field
                label="Quote validity (days)"
                type="number"
                value={String(form.defaultQuoteValidityDays)}
                onChange={(value) =>
                  updateField(
                    "defaultQuoteValidityDays",
                    Number(value || "0"),
                  )
                }
              />
            </div>
          </SettingsCard>

          <SettingsCard
            icon={Landmark}
            title="Banking details"
            description="Stored for business documents and payment instructions."
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                label="Bank name"
                value={form.bankName || ""}
                onChange={(value) => updateField("bankName", value || null)}
              />
              <Field
                label="Account holder"
                value={form.accountHolder || ""}
                onChange={(value) => updateField("accountHolder", value || null)}
              />
              <Field
                label="Account number"
                value={form.accountNumber || ""}
                onChange={(value) => updateField("accountNumber", value || null)}
              />
              <Field
                label="Branch code"
                value={form.branchCode || ""}
                onChange={(value) => updateField("branchCode", value || null)}
              />
            </div>
          </SettingsCard>

          <div className="flex justify-end">
            <button
              type="submit"
              disabled={isSaving}
              className="inline-flex h-11 items-center gap-2 rounded-xl bg-primary px-5 text-sm font-semibold text-primary-foreground disabled:opacity-60"
            >
              {isSaving ? (
                <LoaderCircle className="size-4 animate-spin" />
              ) : (
                <Save className="size-4" />
              )}
              Save business settings
            </button>
          </div>
        </form>

        <div className="mt-6 grid gap-6 xl:grid-cols-2">
          <SettingsCard
            icon={Mail}
            title="Email connection"
            description="Connect Google so QuoteSnap can send invoice PDFs from your business account."
          >
            {googleConnection ? (
              <div className="rounded-2xl border border-border bg-background p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="flex items-center gap-2 text-sm font-semibold">
                      <CheckCircle2 className="size-4 text-primary" />
                      Google connected
                    </p>
                    <p className="mt-2 text-sm">{googleConnection.emailAddress}</p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Connected {formatDate(googleConnection.connectedAt)}
                    </p>
                  </div>
                  <button
                    type="button"
                    disabled={isDisconnectingEmail}
                    onClick={() => void handleDisconnectGoogle()}
                    className="inline-flex h-9 items-center gap-2 rounded-lg border border-border px-3 text-xs font-semibold hover:bg-muted disabled:opacity-50"
                  >
                    {isDisconnectingEmail ? (
                      <LoaderCircle className="size-3.5 animate-spin" />
                    ) : (
                      <Unplug className="size-3.5" />
                    )}
                    Disconnect
                  </button>
                </div>
              </div>
            ) : (
              <div className="rounded-2xl border border-dashed border-border bg-background p-5">
                <p className="text-sm font-semibold">No Google account connected</p>
                <p className="mt-2 text-xs leading-5 text-muted-foreground">
                  Connect an account to enable invoice email delivery through the existing Gmail integration.
                </p>
                <button
                  type="button"
                  disabled={isConnectingEmail}
                  onClick={() => void handleConnectGoogle()}
                  className="mt-4 inline-flex h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-60"
                >
                  {isConnectingEmail ? (
                    <LoaderCircle className="size-4 animate-spin" />
                  ) : (
                    <ExternalLink className="size-4" />
                  )}
                  Connect Google
                </button>
              </div>
            )}

            <button
              type="button"
              onClick={() => void refreshEmailConnections()}
              className="mt-3 inline-flex h-9 items-center gap-2 rounded-lg text-xs font-semibold text-primary hover:underline"
            >
              <RefreshCw className="size-3.5" />
              Refresh connection status
            </button>
          </SettingsCard>

          <SettingsCard
            icon={CreditCard}
            title="Subscription"
            description="Your current QuoteSnap plan and billing status."
          >
            {subscription ? (
              <>
                <div className="rounded-2xl bg-accent p-4">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold text-accent-foreground">
                        CURRENT PLAN
                      </p>
                      <p className="mt-1 text-2xl font-semibold">
                        {planLabels[subscription.plan] || "QuoteSnap"}
                      </p>
                    </div>
                    <span className="rounded-full bg-card px-3 py-1 text-xs font-semibold">
                      {statusLabels[subscription.status] || "Unknown"}
                    </span>
                  </div>

                  {subscription.trialDaysRemaining !== null && (
                    <p className="mt-4 text-sm text-accent-foreground">
                      {subscription.trialDaysRemaining} trial{" "}
                      {subscription.trialDaysRemaining === 1 ? "day" : "days"} remaining
                    </p>
                  )}

                  {subscription.currentPeriodEndsAt && (
                    <p className="mt-3 text-xs text-muted-foreground">
                      Current period ends {formatDate(subscription.currentPeriodEndsAt)}
                    </p>
                  )}
                </div>

                <div className="mt-4 grid gap-3 sm:grid-cols-2">
                  {[2, 3].map((plan) => (
                    <button
                      key={plan}
                      type="button"
                      disabled={
                        upgradingPlan !== null || subscription.plan === plan
                      }
                      onClick={() => void handleUpgrade(plan)}
                      className="rounded-xl border border-border bg-background p-4 text-left transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      <div className="flex items-center gap-2">
                        <BadgeDollarSign className="size-4 text-primary" />
                        <span className="text-sm font-semibold">
                          {planLabels[plan]}
                        </span>
                      </div>
                      <p className="mt-2 text-xs leading-5 text-muted-foreground">
                        {subscription.plan === plan
                          ? "This is your current plan."
                          : "Continue to secure Paystack checkout."}
                      </p>
                      {upgradingPlan === plan && (
                        <LoaderCircle className="mt-3 size-4 animate-spin text-primary" />
                      )}
                    </button>
                  ))}
                </div>
              </>
            ) : (
              <p className="text-sm text-muted-foreground">
                No subscription record is available for this business.
              </p>
            )}
          </SettingsCard>
        </div>
      </div>
    </section>
  )
}

function SettingsCard({
  icon: Icon,
  title,
  description,
  children,
}: {
  icon: typeof Building2
  title: string
  description: string
  children: React.ReactNode
}) {
  return (
    <section className="rounded-2xl border border-border bg-card p-5 sm:p-6">
      <div className="mb-5 flex items-start gap-3">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-accent text-accent-foreground">
          <Icon className="size-4.5" />
        </span>
        <div>
          <h3 className="font-semibold">{title}</h3>
          <p className="mt-1 text-xs leading-5 text-muted-foreground">
            {description}
          </p>
        </div>
      </div>
      {children}
    </section>
  )
}

function Field({
  label,
  value,
  onChange,
  type = "text",
  placeholder = "",
  required = false,
  maxLength,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
  placeholder?: string
  required?: boolean
  maxLength?: number
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium">{label}</span>
      <input
        type={type}
        value={value}
        required={required}
        maxLength={maxLength}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
      />
    </label>
  )
}

function stripId(settings: BusinessSettings): BusinessSettingsInput {
  const { id: _id, ...input } = settings
  return input
}

function clean(value: string | null) {
  return value?.trim() || null
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-ZA", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(new Date(value))
}
