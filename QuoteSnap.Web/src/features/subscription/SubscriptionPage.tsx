import { useEffect, useMemo, useState } from "react"
import {
  ArrowUpRight,
  BadgeDollarSign,
  Check,
  CreditCard,
  ExternalLink,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  X,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { subscriptionApi } from "./subscription.api"
import type {
  SubscriptionOverview,
  SubscriptionPayment,
  SubscriptionPlanDefinition,
} from "./subscription.types"

const statusLabels: Record<number, string> = {
  1: "Trial",
  2: "Active",
  3: "Past due",
  4: "Cancelled",
  5: "Expired",
}

const paymentStatusLabels: Record<number, string> = {
  1: "Pending",
  2: "Successful",
  3: "Failed",
  4: "Cancelled",
  5: "Refunded",
}

export function SubscriptionPage({
  onSubscriptionChanged,
}: {
  onSubscriptionChanged?: () => void
}) {
  const [overview, setOverview] = useState<SubscriptionOverview | null>(null)
  const [payments, setPayments] = useState<SubscriptionPayment[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [changingPlan, setChangingPlan] = useState<number | null>(null)
  const [isCancelling, setIsCancelling] = useState(false)
  const [isOpeningBilling, setIsOpeningBilling] = useState(false)
  const [error, setError] = useState("")
  const [success, setSuccess] = useState("")

  useEffect(() => {
    void loadData()
  }, [])

  async function loadData() {
    try {
      setIsLoading(true)
      setError("")

      const [overviewData, paymentData] = await Promise.all([
        subscriptionApi.getOverview(),
        subscriptionApi.getPayments(),
      ])

      setOverview(overviewData)
      setPayments(paymentData)
      onSubscriptionChanged?.()

      const params = new URLSearchParams(window.location.search)

      if (params.get("subscription") === "return") {
        setSuccess(
          params.get("status") === "success"
            ? "Subscription payment confirmed. Your plan details have been refreshed."
            : "Payment return received. Paystack verification may still be processing.",
        )

        window.history.replaceState(
          {},
          "",
          `${window.location.pathname}?view=subscription`,
        )
      }
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load subscription information.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const currentPlan = overview?.subscription.plan ?? 1
  const currentStatus = overview?.subscription.status ?? 5
  const isProTrial = overview?.isTrialUsingProAccess ?? false
  const currentAccessName = isProTrial
    ? `${overview?.effectivePlan.name ?? "Pro"} trial`
    : planName(currentPlan)

  const renewalMessage = useMemo(() => {
    if (!overview) return ""

    const subscription = overview.subscription

    if (subscription.status === 1 && subscription.trialEndsAt) {
      return `Trial ends ${formatDate(subscription.trialEndsAt)}`
    }

    if (subscription.status === 4 && subscription.currentPeriodEndsAt) {
      return `Access remains active until ${formatDate(
        subscription.currentPeriodEndsAt,
      )}`
    }

    if (subscription.status === 2 && subscription.currentPeriodEndsAt) {
      return `Renews ${formatDate(subscription.currentPeriodEndsAt)}`
    }

    if (subscription.status === 3) {
      return "Payment requires attention"
    }

    if (subscription.status === 5) {
      return "Subscription access has expired"
    }

    return ""
  }, [overview])

  const handleChangePlan = async (plan: number) => {
    try {
      setChangingPlan(plan)
      setError("")
      setSuccess("")
      const result = await subscriptionApi.changePlan(plan)
      window.location.assign(result.authorizationUrl)
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't start plan checkout.",
      )
      setChangingPlan(null)
    }
  }

  const handleCancel = async () => {
    if (
      !window.confirm(
        "Cancel renewal? Your paid access will remain available until the end of the current billing period.",
      )
    ) {
      return
    }

    try {
      setIsCancelling(true)
      setError("")
      setSuccess("")
      await subscriptionApi.cancel()
      setSuccess(
        "Subscription renewal cancelled. Your current paid access remains available until the period ends.",
      )
      await loadData()
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't cancel the subscription.",
      )
    } finally {
      setIsCancelling(false)
    }
  }

  const handleManageBilling = async () => {
    try {
      setIsOpeningBilling(true)
      setError("")
      const result = await subscriptionApi.getManageLink()
      window.location.assign(result.url)
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't open Paystack subscription management.",
      )
      setIsOpeningBilling(false)
    }
  }

  if (isLoading) {
    return (
      <section className="flex min-h-[60vh] items-center justify-center">
        <LoaderCircle className="size-6 animate-spin text-primary" />
      </section>
    )
  }

  if (!overview) {
    return (
      <section className="px-4 py-10 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-xl rounded-2xl border border-border bg-card p-6 text-center">
          <ShieldAlert className="mx-auto size-7 text-muted-foreground" />
          <p className="mt-3 text-lg font-semibold">
            Subscription information could not be loaded
          </p>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">
            {error || "The subscription service did not return a usable response."}
          </p>
          <button
            type="button"
            onClick={() => void loadData()}
            className="mt-5 inline-flex h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground"
          >
            <RefreshCw className="size-4" />
            Retry
          </button>
        </div>
      </section>
    )
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1300px]">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-sm font-semibold text-primary">SUBSCRIPTION</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
              Plans, billing & usage
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Manage your QuoteSnap plan, limits, renewal and billing history.
            </p>
          </div>

          <button
            type="button"
            onClick={() => void loadData()}
            className="inline-flex h-10 items-center gap-2 self-start rounded-xl border border-border bg-card px-4 text-sm font-semibold hover:bg-muted"
          >
            <RefreshCw className="size-4" />
            Refresh
          </button>
        </div>

        {(error || success) && (
          <div
            className="mt-6 flex items-start gap-3 rounded-xl px-4 py-3 text-sm"
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
            {error ? (
              <X className="mt-0.5 size-4 shrink-0" />
            ) : (
              <Check className="mt-0.5 size-4 shrink-0" />
            )}
            <span>{error || success}</span>
          </div>
        )}

        <div className="mt-8 grid gap-6 xl:grid-cols-[1.25fr_0.75fr]">
          <section className="rounded-2xl border border-border bg-card p-5 sm:p-6">
            <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-start">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.12em] text-muted-foreground">
                  Current plan
                </p>
                <div className="mt-2 flex flex-wrap items-center gap-3">
                  <h3 className="text-3xl font-semibold">
                    {currentAccessName}
                  </h3>
                  <span className="rounded-full bg-accent px-3 py-1 text-xs font-semibold text-accent-foreground">
                    {statusLabels[currentStatus] || "Unknown"}
                  </span>
                </div>
                {overview.isTrialUsingProAccess && (
                  <p className="mt-2 text-sm font-medium text-primary">
                    Pro limits are active during your trial. Your account moves to Free plan limits when the trial ends.
                  </p>
                )}
                <p className="mt-2 text-sm text-muted-foreground">
                  {renewalMessage}
                </p>
              </div>

              <div className="flex flex-wrap gap-2">
                {currentPlan !== 1 &&
                  currentStatus !== 4 &&
                  currentStatus !== 5 && (
                    <button
                      type="button"
                      disabled={isCancelling}
                      onClick={() => void handleCancel()}
                      className="inline-flex h-10 items-center gap-2 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-muted disabled:opacity-50"
                    >
                      {isCancelling && (
                        <LoaderCircle className="size-4 animate-spin" />
                      )}
                      Cancel renewal
                    </button>
                  )}

                {currentPlan !== 1 && (
                  <button
                    type="button"
                    disabled={isOpeningBilling}
                    onClick={() => void handleManageBilling()}
                    className="inline-flex h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-50"
                  >
                    {isOpeningBilling ? (
                      <LoaderCircle className="size-4 animate-spin" />
                    ) : (
                      <ExternalLink className="size-4" />
                    )}
                    Manage billing
                  </button>
                )}
              </div>
            </div>

            {overview.accessMessage && (
              <div
                className="mt-5 flex gap-3 rounded-xl px-4 py-3 text-sm"
                style={{
                  color: overview.canWrite
                    ? "var(--status-warning)"
                    : "var(--status-danger)",
                  backgroundColor: overview.canWrite
                    ? "var(--status-warning-bg)"
                    : "var(--status-danger-bg)",
                }}
              >
                <ShieldAlert className="mt-0.5 size-4 shrink-0" />
                {overview.accessMessage}
              </div>
            )}
          </section>

          <section className="rounded-2xl border border-border bg-card p-5 sm:p-6">
            <p className="text-xs font-semibold uppercase tracking-[0.12em] text-muted-foreground">
              Trial / renewal status
            </p>

            {overview.subscription.status === 1 ? (
              <>
                <p className="mt-3 text-3xl font-semibold">
                  {overview.subscription.trialDaysRemaining ?? 0}
                </p>
                <p className="mt-1 text-sm text-muted-foreground">
                  trial days remaining
                </p>
              </>
            ) : (
              <>
                <p className="mt-3 text-lg font-semibold">
                  {renewalMessage || statusLabels[currentStatus]}
                </p>
                <p className="mt-2 text-xs leading-5 text-muted-foreground">
                  Billing status is updated from Paystack webhook events.
                </p>
              </>
            )}
          </section>
        </div>

        <section className="mt-6 rounded-2xl border border-border bg-card p-5 sm:p-6">
          <div>
            <h3 className="font-semibold">Current usage</h3>
            <p className="mt-1 text-xs text-muted-foreground">
              Usage is measured against the limits currently active on your account.
            </p>
          </div>

          <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
            <UsageCard
              label="Customers"
              used={overview.usage.customers}
              limit={overview.effectivePlan.customerLimit}
            />
            <UsageCard
              label="Catalogue"
              used={overview.usage.catalogueItems}
              limit={overview.effectivePlan.catalogueItemLimit}
            />
            <UsageCard
              label="Quotes this month"
              used={overview.usage.quotesThisMonth}
              limit={overview.effectivePlan.monthlyQuoteLimit}
            />
            <UsageCard
              label="Invoices this month"
              used={overview.usage.invoicesThisMonth}
              limit={overview.effectivePlan.monthlyInvoiceLimit}
            />
            <UsageCard
              label="Receipts this month"
              used={overview.usage.receiptsThisMonth}
              limit={overview.effectivePlan.monthlyReceiptLimit}
            />
          </div>
        </section>

        <section className="mt-6">
          <div>
            <h3 className="text-xl font-semibold tracking-[-0.025em]">
              Compare plans
            </h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Prices and limits come from the QuoteSnap backend configuration.
            </p>
          </div>

          <div className="mt-5 grid gap-4 lg:grid-cols-3">
            {overview.plans.map((plan) => (
              <PlanCard
                key={plan.plan}
                plan={plan}
                currentPlan={currentPlan}
                currentStatus={currentStatus}
                effectivePlan={overview.effectivePlan.plan}
                isTrialUsingProAccess={overview.isTrialUsingProAccess}
                changingPlan={changingPlan}
                onChoose={handleChangePlan}
              />
            ))}
          </div>
        </section>

        <section className="mt-6 rounded-2xl border border-border bg-card">
          <div className="flex items-center justify-between border-b border-border px-5 py-4">
            <div>
              <h3 className="font-semibold">Billing history</h3>
              <p className="mt-1 text-xs text-muted-foreground">
                Initial checkouts, renewals and failed recurring payments.
              </p>
            </div>
            <CreditCard className="size-5 text-muted-foreground" />
          </div>

          {payments.length === 0 ? (
            <div className="flex min-h-48 flex-col items-center justify-center px-6 text-center">
              <BadgeDollarSign className="size-7 text-muted-foreground" />
              <p className="mt-3 text-sm font-semibold">No billing activity yet</p>
              <p className="mt-1 text-xs text-muted-foreground">
                Subscription payments will appear here.
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[820px] text-left">
                <thead>
                  <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                    <th className="px-5 py-3 font-medium">Date</th>
                    <th className="px-5 py-3 font-medium">Plan</th>
                    <th className="px-5 py-3 font-medium">Status</th>
                    <th className="px-5 py-3 font-medium">Reference</th>
                    <th className="px-5 py-3 text-right font-medium">Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {payments.map((payment) => (
                    <tr
                      key={payment.id}
                      className="border-b border-border last:border-0"
                    >
                      <td className="px-5 py-4 text-xs text-muted-foreground">
                        {formatDate(payment.paidAt || payment.createdAt)}
                      </td>
                      <td className="px-5 py-4 text-sm font-semibold">
                        {planName(payment.plan)}
                      </td>
                      <td className="px-5 py-4">
                        <span
                          className="rounded-full px-2.5 py-1 text-xs font-semibold"
                          style={paymentStatusStyle(payment.status)}
                        >
                          {paymentStatusLabels[payment.status] || "Unknown"}
                        </span>
                      </td>
                      <td className="px-5 py-4 text-xs text-muted-foreground">
                        {payment.reference || "—"}
                      </td>
                      <td className="px-5 py-4 text-right text-sm font-semibold">
                        {formatMoney(
                          payment.amount,
                          payment.currencyCode,
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </section>
  )
}

function PlanCard({
  plan,
  currentPlan,
  currentStatus,
  effectivePlan,
  isTrialUsingProAccess,
  changingPlan,
  onChoose,
}: {
  plan: SubscriptionPlanDefinition
  currentPlan: number
  currentStatus: number
  effectivePlan: number
  isTrialUsingProAccess: boolean
  changingPlan: number | null
  onChoose: (plan: number) => Promise<void>
}) {
  const samePlan = currentPlan === plan.plan
  const isCurrent =
    !isTrialUsingProAccess &&
    samePlan &&
    currentStatus === 2

  const isTrialPlan =
    isTrialUsingProAccess &&
    effectivePlan === plan.plan

  const isFreeAfterTrial =
    isTrialUsingProAccess &&
    plan.plan === 1

  const isPaid = plan.plan === 2 || plan.plan === 3
  const isUpgrade = plan.plan > currentPlan

  const actionLabel =
    samePlan && currentStatus === 3
      ? "Retry payment"
      : samePlan && currentStatus === 4
        ? "Resume plan"
        : samePlan && currentStatus === 5
          ? "Renew plan"
          : isUpgrade
            ? "Upgrade"
            : "Change to this plan"

  return (
    <article
      className={`rounded-2xl border p-5 ${
        isCurrent
          ? "border-primary bg-accent/40"
          : "border-border bg-card"
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <h4 className="text-lg font-semibold">{plan.name}</h4>
          <p className="mt-2 text-2xl font-semibold">
            {plan.monthlyPrice === 0
              ? "Free"
              : formatMoney(plan.monthlyPrice, plan.currencyCode)}
          </p>
          {plan.monthlyPrice > 0 && (
            <p className="mt-1 text-xs text-muted-foreground">per month</p>
          )}
        </div>

        {isCurrent && (
          <span className="rounded-full bg-primary px-2.5 py-1 text-[11px] font-semibold text-primary-foreground">
            Current
          </span>
        )}
        {isTrialPlan && (
          <span className="rounded-full bg-primary px-2.5 py-1 text-[11px] font-semibold text-primary-foreground">
            Trial access
          </span>
        )}
        {isFreeAfterTrial && (
          <span className="rounded-full bg-muted px-2.5 py-1 text-[11px] font-semibold text-muted-foreground">
            After trial
          </span>
        )}
      </div>

      <div className="mt-5 space-y-2.5 text-xs">
        <Feature value={limitText(plan.customerLimit, "customers")} />
        <Feature value={limitText(plan.catalogueItemLimit, "catalogue items")} />
        <Feature value={limitText(plan.monthlyQuoteLimit, "quotes / month")} />
        <Feature value={limitText(plan.monthlyInvoiceLimit, "invoices / month")} />
        <Feature value={limitText(plan.monthlyReceiptLimit, "receipts / month")} />
        <Feature value="PDF documents" />
        <Feature
          value={
            plan.emailDelivery
              ? "Invoice email delivery"
              : "No invoice email delivery"
          }
          enabled={plan.emailDelivery}
        />
      </div>

      <div className="mt-6">
        {isCurrent ? (
          <button
            type="button"
            disabled
            className="h-10 w-full rounded-xl border border-border text-sm font-semibold text-muted-foreground"
          >
            Current plan
          </button>
        ) : isPaid ? (
          <button
            type="button"
            disabled={changingPlan !== null}
            onClick={() => void onChoose(plan.plan)}
            className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-50"
          >
            {changingPlan === plan.plan ? (
              <LoaderCircle className="size-4 animate-spin" />
            ) : (
              <ArrowUpRight className="size-4" />
            )}
            {actionLabel}
          </button>
        ) : (
          <p className="rounded-xl bg-muted px-3 py-2.5 text-center text-xs leading-5 text-muted-foreground">
            {isFreeAfterTrial
              ? "Your account moves to these Free limits when the trial ends."
              : "Paid plans return to Free after cancellation or expiry."}
          </p>
        )}
      </div>
    </article>
  )
}

function UsageCard({
  label,
  used,
  limit,
}: {
  label: string
  used: number
  limit: number | null
}) {
  const percentage =
    limit === null || limit === 0
      ? 0
      : Math.min(100, (used / limit) * 100)

  return (
    <article className="rounded-xl border border-border bg-background p-4">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-2 text-lg font-semibold">
        {used}
        <span className="text-sm font-normal text-muted-foreground">
          {limit === null ? " / Unlimited" : ` / ${limit}`}
        </span>
      </p>
      {limit !== null && (
        <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-muted">
          <div
            className="h-full rounded-full bg-primary"
            style={{ width: `${percentage}%` }}
          />
        </div>
      )}
    </article>
  )
}

function Feature({
  value,
  enabled = true,
}: {
  value: string
  enabled?: boolean
}) {
  return (
    <div
      className={`flex items-center gap-2 ${
        enabled ? "text-foreground" : "text-muted-foreground"
      }`}
    >
      {enabled ? (
        <Check className="size-3.5 text-primary" />
      ) : (
        <X className="size-3.5" />
      )}
      {value}
    </div>
  )
}

function limitText(limit: number | null, label: string) {
  return limit === null ? `Unlimited ${label}` : `${limit} ${label}`
}

function planName(plan: number) {
  return (
    {
      1: "Free",
      2: "Pro",
      3: "Business",
    }[plan] || "QuoteSnap"
  )
}

function formatMoney(amount: number, currencyCode: string) {
  return new Intl.NumberFormat("en-ZA", {
    style: "currency",
    currency: currencyCode || "ZAR",
  }).format(amount)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-ZA", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(new Date(value))
}

function paymentStatusStyle(status: number) {
  if (status === 2) {
    return {
      color: "var(--status-success)",
      backgroundColor: "var(--status-success-bg)",
    }
  }

  if (status === 3) {
    return {
      color: "var(--status-danger)",
      backgroundColor: "var(--status-danger-bg)",
    }
  }

  return {
    color: "var(--status-warning)",
    backgroundColor: "var(--status-warning-bg)",
  }
}
