import { useEffect, useMemo, useState } from "react"
import {
  CircleDollarSign,
  FileCheck2,
  FileText,
  LoaderCircle,
  Plus,
  ReceiptText,
  Users,
  WalletCards,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import {
  getDashboardData,
  type DashboardData,
  type DashboardInvoice,
} from "./dashboard.api"

type DashboardOverviewProps = {
  displayName: string
  onNavigate: (destination: string) => void
}

const invoiceStatuses: Record<
  number,
  { label: string; color: string; background: string }
> = {
  1: {
    label: "Draft",
    color: "var(--status-draft)",
    background: "var(--status-draft-bg)",
  },
  2: {
    label: "Sent",
    color: "var(--status-sent)",
    background: "var(--status-sent-bg)",
  },
  3: {
    label: "Partially paid",
    color: "var(--status-warning)",
    background: "var(--status-warning-bg)",
  },
  4: {
    label: "Paid",
    color: "var(--status-success)",
    background: "var(--status-success-bg)",
  },
  5: {
    label: "Overdue",
    color: "var(--status-danger)",
    background: "var(--status-danger-bg)",
  },
  6: {
    label: "Cancelled",
    color: "var(--status-draft)",
    background: "var(--status-draft-bg)",
  },
}

export function DashboardOverview({
  displayName,
  onNavigate,
}: DashboardOverviewProps) {
  const [data, setData] = useState<DashboardData>({
    invoices: [],
    customers: [],
  })
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState("")

  useEffect(() => {
    void loadDashboard()
  }, [])

  async function loadDashboard() {
    try {
      setIsLoading(true)
      setError("")
      setData(await getDashboardData())
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your dashboard.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const summary = useMemo(() => {
    const currencyCode = data.invoices[0]?.currencyCode || "ZAR"

    return {
      currencyCode,
      revenue: data.invoices.reduce(
        (total, invoice) => total + invoice.amountPaid,
        0,
      ),
      outstanding: data.invoices.reduce(
        (total, invoice) => total + invoice.balanceDue,
        0,
      ),
      invoiceCount: data.invoices.length,
      customerCount: data.customers.length,
      recentInvoices: [...data.invoices]
        .sort(
          (left, right) =>
            new Date(right.createdAt).getTime() -
            new Date(left.createdAt).getTime(),
        )
        .slice(0, 5),
    }
  }, [data])

  const cards = [
    {
      label: "Revenue",
      value: formatMoney(summary.revenue, summary.currencyCode),
      helper: "Payments received",
      icon: CircleDollarSign,
    },
    {
      label: "Outstanding",
      value: formatMoney(summary.outstanding, summary.currencyCode),
      helper: "Still to be collected",
      icon: WalletCards,
    },
    {
      label: "Invoices",
      value: summary.invoiceCount.toString(),
      helper: "Total invoices",
      icon: FileCheck2,
    },
    {
      label: "Customers",
      value: summary.customerCount.toString(),
      helper: "Saved customers",
      icon: Users,
    },
  ]

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-sm font-semibold text-primary">OVERVIEW</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
              Good to see you, {displayName}.
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Live totals from your QuoteSnap business.
            </p>
          </div>
          <button
            type="button"
            onClick={() => onNavigate("Invoices")}
            className="inline-flex h-10 items-center justify-center gap-2 self-start rounded-xl border border-border bg-card px-4 text-sm font-semibold transition hover:bg-muted sm:hidden"
          >
            <Plus className="size-4" />
            New invoice
          </button>
        </div>

        {error && (
          <div
            className="mt-6 flex items-center justify-between gap-4 rounded-xl px-4 py-3 text-sm"
            style={{
              color: "var(--status-danger)",
              backgroundColor: "var(--status-danger-bg)",
            }}
          >
            <span>{error}</span>
            <button
              type="button"
              onClick={() => void loadDashboard()}
              className="font-semibold underline"
            >
              Retry
            </button>
          </div>
        )}

        {isLoading ? (
          <div className="mt-8 flex min-h-72 items-center justify-center rounded-2xl border border-border bg-card">
            <LoaderCircle className="size-6 animate-spin text-primary" />
          </div>
        ) : (
          <>
            <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              {cards.map(({ label, value, helper, icon: Icon }) => (
                <article
                  key={label}
                  className="rounded-2xl border border-border bg-card p-5"
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="text-sm text-muted-foreground">{label}</p>
                      <p className="mt-3 text-2xl font-semibold tracking-[-0.035em]">
                        {value}
                      </p>
                    </div>
                    <span className="flex size-10 items-center justify-center rounded-xl bg-accent text-accent-foreground">
                      <Icon className="size-[18px]" />
                    </span>
                  </div>
                  <p className="mt-4 text-xs text-muted-foreground">{helper}</p>
                </article>
              ))}
            </div>

            <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.55fr)_minmax(320px,0.7fr)]">
              <RecentInvoices
                invoices={summary.recentInvoices}
                onNavigate={onNavigate}
              />

              <section className="rounded-2xl border border-border bg-card p-5">
                <h3 className="font-semibold">Quick actions</h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  Jump straight into common QuoteSnap tasks.
                </p>

                <div className="mt-5 space-y-2">
                  {[
                    { label: "Create a quote", destination: "Quotes", icon: FileText },
                    { label: "Add a customer", destination: "Customers", icon: Users },
                    { label: "Record a payment", destination: "Payments", icon: WalletCards },
                    { label: "Create a receipt", destination: "Receipts", icon: ReceiptText },
                  ].map(({ label, destination, icon: Icon }) => (
                    <button
                      key={label}
                      type="button"
                      onClick={() => onNavigate(destination)}
                      className="flex w-full items-center gap-3 rounded-xl border border-border bg-background px-3.5 py-3 text-left text-sm font-medium transition hover:bg-muted"
                    >
                      <span className="flex size-9 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                        <Icon className="size-4" />
                      </span>
                      {label}
                    </button>
                  ))}
                </div>
              </section>
            </div>
          </>
        )}
      </div>
    </section>
  )
}

function RecentInvoices({
  invoices,
  onNavigate,
}: {
  invoices: DashboardInvoice[]
  onNavigate: (destination: string) => void
}) {
  return (
    <section className="rounded-2xl border border-border bg-card">
      <div className="flex items-center justify-between border-b border-border px-5 py-4">
        <div>
          <h3 className="font-semibold">Recent invoices</h3>
          <p className="mt-1 text-xs text-muted-foreground">
            Your latest invoice activity.
          </p>
        </div>
        <button
          type="button"
          onClick={() => onNavigate("Invoices")}
          className="text-xs font-semibold text-primary hover:underline"
        >
          View all
        </button>
      </div>

      {invoices.length === 0 ? (
        <div className="flex min-h-64 flex-col items-center justify-center px-6 py-10 text-center">
          <span className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
            <FileCheck2 className="size-5" />
          </span>
          <p className="mt-4 text-sm font-semibold">No invoices yet</p>
          <p className="mt-1 max-w-xs text-xs leading-5 text-muted-foreground">
            Once you create an invoice, its payment status will appear here.
          </p>
          <button
            type="button"
            onClick={() => onNavigate("Invoices")}
            className="mt-5 inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
          >
            <Plus className="size-3.5" />
            Go to invoices
          </button>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px] text-left">
            <thead>
              <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                <th className="px-5 py-3 font-medium">Invoice</th>
                <th className="px-5 py-3 font-medium">Customer</th>
                <th className="px-5 py-3 font-medium">Status</th>
                <th className="px-5 py-3 text-right font-medium">Balance</th>
              </tr>
            </thead>
            <tbody>
              {invoices.map((invoice) => {
                const status =
                  invoiceStatuses[invoice.status] || invoiceStatuses[1]

                return (
                  <tr
                    key={invoice.id}
                    className="border-b border-border last:border-0"
                  >
                    <td className="px-5 py-4">
                      <p className="text-sm font-semibold">
                        {invoice.invoiceNumber}
                      </p>
                      <p className="mt-1 text-xs text-muted-foreground">
                        {new Intl.DateTimeFormat("en-ZA", {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                        }).format(new Date(invoice.issueDate))}
                      </p>
                    </td>
                    <td className="px-5 py-4 text-sm">
                      {invoice.customerName}
                    </td>
                    <td className="px-5 py-4">
                      <span
                        className="rounded-full px-2.5 py-1 text-xs font-semibold"
                        style={{
                          color: status.color,
                          backgroundColor: status.background,
                        }}
                      >
                        {status.label}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-right text-sm font-semibold">
                      {formatMoney(invoice.balanceDue, invoice.currencyCode)}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

function formatMoney(amount: number, currencyCode: string) {
  try {
    return new Intl.NumberFormat("en-ZA", {
      style: "currency",
      currency: currencyCode || "ZAR",
      maximumFractionDigits: 2,
    }).format(amount)
  } catch {
    return `R ${amount.toFixed(2)}`
  }
}
