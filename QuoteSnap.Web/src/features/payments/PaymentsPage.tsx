import { useEffect, useMemo, useState, type FormEvent } from "react"
import {
  CheckCircle2,
  FileText,
  LoaderCircle,
  Plus,
  ReceiptText,
  Search,
  WalletCards,
  X,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { invoiceApi } from "@/features/invoices/invoice.api"
import {
  InvoiceStatus,
  type Invoice,
} from "@/features/invoices/invoice.types"
import { receiptApi } from "@/features/receipts/receipt.api"
import type { Receipt } from "@/features/receipts/receipt.types"
import { paymentApi } from "./payment.api"
import {
  PaymentMethod,
  type Payment,
} from "./payment.types"

type PaymentRow = Payment & {
  invoiceNumber: string
  customerName: string
  currencyCode: string
}

const methodLabels: Record<number, string> = {
  [PaymentMethod.Cash]: "Cash",
  [PaymentMethod.BankTransfer]: "Bank transfer",
  [PaymentMethod.Card]: "Card",
  [PaymentMethod.MobileMoney]: "Mobile money",
  [PaymentMethod.Other]: "Other",
}

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function PaymentsPage({
  onNavigate,
}: {
  onNavigate: (destination: string) => void
}) {
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [payments, setPayments] = useState<PaymentRow[]>([])
  const [receipts, setReceipts] = useState<Receipt[]>([])
  const [search, setSearch] = useState("")
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState("")
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null)
  const [amount, setAmount] = useState("")
  const [method, setMethod] = useState<number>(PaymentMethod.BankTransfer)
  const [paymentDate, setPaymentDate] = useState(today())
  const [reference, setReference] = useState("")
  const [notes, setNotes] = useState("")
  const [isSaving, setIsSaving] = useState(false)
  const [formError, setFormError] = useState("")
  const [generatingReceiptId, setGeneratingReceiptId] = useState<string | null>(
    null,
  )

  useEffect(() => {
    void loadData()
  }, [])

  async function loadData() {
    try {
      setIsLoading(true)
      setError("")

      const [invoiceRows, receiptRows] = await Promise.all([
        invoiceApi.getAll(),
        receiptApi.getAll(),
      ])

      const paymentGroups = await Promise.all(
        invoiceRows.map(async (invoice) => {
          const rows = await paymentApi.getForInvoice(invoice.id)

          return rows.map((payment) => ({
            ...payment,
            invoiceNumber: invoice.invoiceNumber,
            customerName: invoice.customerName,
            currencyCode: invoice.currencyCode,
          }))
        }),
      )

      setInvoices(invoiceRows)
      setReceipts(receiptRows)
      setPayments(
        paymentGroups
          .flat()
          .sort(
            (left, right) =>
              new Date(right.paymentDate).getTime() -
              new Date(left.paymentDate).getTime(),
          ),
      )
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your payments.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const outstandingInvoices = useMemo(
    () =>
      invoices.filter(
        (invoice) =>
          invoice.balanceDue > 0 &&
          invoice.status !== InvoiceStatus.Paid &&
          invoice.status !== InvoiceStatus.Cancelled,
      ),
    [invoices],
  )

  const receiptPaymentIds = useMemo(
    () => new Set(receipts.map((receipt) => receipt.paymentId)),
    [receipts],
  )

  const filteredPayments = useMemo(() => {
    const query = search.trim().toLowerCase()
    if (!query) return payments

    return payments.filter((payment) =>
      [
        payment.invoiceNumber,
        payment.customerName,
        payment.reference,
        methodLabels[payment.method],
      ]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query)),
    )
  }, [payments, search])

  const openPayment = (invoice: Invoice) => {
    setSelectedInvoice(invoice)
    setAmount(invoice.balanceDue.toFixed(2))
    setMethod(PaymentMethod.BankTransfer)
    setPaymentDate(today())
    setReference("")
    setNotes("")
    setFormError("")
  }

  const closePayment = () => {
    if (!isSaving) {
      setSelectedInvoice(null)
      setFormError("")
    }
  }

  const handlePayment = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!selectedInvoice) return

    const paymentAmount = Number(amount)

    if (
      Number.isNaN(paymentAmount) ||
      paymentAmount <= 0 ||
      paymentAmount > selectedInvoice.balanceDue
    ) {
      setFormError(
        `Enter an amount between 0.01 and ${formatMoney(
          selectedInvoice.balanceDue,
          selectedInvoice.currencyCode,
        )}.`,
      )
      return
    }

    try {
      setIsSaving(true)
      setFormError("")

      const updatedInvoice = await paymentApi.record(selectedInvoice.id, {
        amount: paymentAmount,
        method,
        paymentDate: paymentDate
          ? paymentDate === today()
            ? new Date().toISOString()
            : new Date(`${paymentDate}T12:00:00`).toISOString()
          : null,
        reference: reference.trim() || null,
        notes: notes.trim() || null,
      })

      const refreshedPayments = await paymentApi.getForInvoice(
        selectedInvoice.id,
      )

      setInvoices((current) =>
        current.map((invoice) =>
          invoice.id === updatedInvoice.id ? updatedInvoice : invoice,
        ),
      )

      setPayments((current) => {
        const withoutInvoice = current.filter(
          (payment) => payment.invoiceId !== selectedInvoice.id,
        )

        const mapped = refreshedPayments.map((payment) => ({
          ...payment,
          invoiceNumber: updatedInvoice.invoiceNumber,
          customerName: updatedInvoice.customerName,
          currencyCode: updatedInvoice.currencyCode,
        }))

        return [...mapped, ...withoutInvoice].sort(
          (left, right) =>
            new Date(right.paymentDate).getTime() -
            new Date(left.paymentDate).getTime(),
        )
      })

      setSelectedInvoice(null)
    } catch (error) {
      setFormError(
        error instanceof ApiError
          ? error.message
          : "We couldn't record this payment.",
      )
    } finally {
      setIsSaving(false)
    }
  }

  const handleGenerateReceipt = async (payment: PaymentRow) => {
    try {
      setGeneratingReceiptId(payment.id)
      setError("")
      const receipt = await receiptApi.generateFromPayment(payment.id)
      setReceipts((current) => [receipt, ...current])
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't generate the receipt.",
      )
    } finally {
      setGeneratingReceiptId(null)
    }
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div>
          <p className="text-sm font-semibold text-primary">PAYMENTS</p>
          <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
            Payments & balances
          </h2>
          <p className="mt-2 text-sm text-muted-foreground">
            Record customer payments and turn each payment into a receipt.
          </p>
        </div>

        <section className="mt-8 rounded-2xl border border-border bg-card">
          <div className="flex items-center justify-between border-b border-border px-5 py-4">
            <div>
              <h3 className="font-semibold">Outstanding invoices</h3>
              <p className="mt-1 text-xs text-muted-foreground">
                Record full or partial payments against open balances.
              </p>
            </div>
            <span className="rounded-full bg-muted px-2.5 py-1 text-xs font-semibold text-muted-foreground">
              {outstandingInvoices.length}
            </span>
          </div>

          {isLoading ? (
            <div className="flex min-h-44 items-center justify-center">
              <LoaderCircle className="size-6 animate-spin text-primary" />
            </div>
          ) : outstandingInvoices.length === 0 ? (
            <div className="flex min-h-44 flex-col items-center justify-center px-6 py-8 text-center">
              <CheckCircle2 className="size-6 text-primary" />
              <p className="mt-3 text-sm font-semibold">No open balances</p>
              <p className="mt-1 text-xs text-muted-foreground">
                Outstanding invoices will appear here.
              </p>
            </div>
          ) : (
            <div className="grid gap-3 p-4 lg:grid-cols-2">
              {outstandingInvoices.map((invoice) => (
                <article
                  key={invoice.id}
                  className="flex flex-col justify-between gap-4 rounded-xl border border-border bg-background p-4 sm:flex-row sm:items-center"
                >
                  <div>
                    <p className="text-sm font-semibold">
                      {invoice.invoiceNumber}
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {invoice.customerName}
                    </p>
                    <p className="mt-2 text-sm font-semibold text-primary">
                      {formatMoney(invoice.balanceDue, invoice.currencyCode)} due
                    </p>
                  </div>

                  <button
                    type="button"
                    onClick={() => openPayment(invoice)}
                    className="inline-flex h-9 items-center justify-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
                  >
                    <Plus className="size-3.5" />
                    Record payment
                  </button>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="mt-6 rounded-2xl border border-border bg-card">
          <div className="flex flex-col gap-4 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative w-full sm:max-w-sm">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search payment history..."
                className="h-10 w-full rounded-xl border border-input bg-background pl-10 pr-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
              />
            </div>
            <p className="text-xs text-muted-foreground">
              {payments.length} {payments.length === 1 ? "payment" : "payments"}
            </p>
          </div>

          {error && (
            <div
              className="m-4 rounded-xl px-4 py-3 text-sm"
              style={{
                color: "var(--status-danger)",
                backgroundColor: "var(--status-danger-bg)",
              }}
            >
              {error}
            </div>
          )}

          {!isLoading && filteredPayments.length === 0 ? (
            <div className="flex min-h-64 flex-col items-center justify-center px-6 py-10 text-center">
              <WalletCards className="size-7 text-muted-foreground" />
              <p className="mt-3 text-sm font-semibold">
                {search ? "No matching payments" : "No payments recorded yet"}
              </p>
            </div>
          ) : !isLoading ? (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[1020px] text-left">
                <thead>
                  <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                    <th className="px-5 py-3 font-medium">Invoice</th>
                    <th className="px-5 py-3 font-medium">Customer</th>
                    <th className="px-5 py-3 font-medium">Method</th>
                    <th className="px-5 py-3 font-medium">Date</th>
                    <th className="px-5 py-3 font-medium">Reference</th>
                    <th className="px-5 py-3 text-right font-medium">Amount</th>
                    <th className="px-5 py-3 text-right font-medium">Receipt</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredPayments.map((payment) => {
                    const hasReceipt = receiptPaymentIds.has(payment.id)

                    return (
                      <tr
                        key={payment.id}
                        className="border-b border-border last:border-0 hover:bg-muted/30"
                      >
                        <td className="px-5 py-4 text-sm font-semibold">
                          {payment.invoiceNumber}
                        </td>
                        <td className="px-5 py-4 text-sm">
                          {payment.customerName}
                        </td>
                        <td className="px-5 py-4 text-xs text-muted-foreground">
                          {methodLabels[payment.method] || "Other"}
                        </td>
                        <td className="px-5 py-4 text-xs text-muted-foreground">
                          {formatDate(payment.paymentDate)}
                        </td>
                        <td className="px-5 py-4 text-xs text-muted-foreground">
                          {payment.reference || "—"}
                        </td>
                        <td className="px-5 py-4 text-right text-sm font-semibold">
                          {formatMoney(payment.amount, payment.currencyCode)}
                        </td>
                        <td className="px-5 py-4 text-right">
                          {hasReceipt ? (
                            <button
                              type="button"
                              onClick={() => onNavigate("Receipts")}
                              className="inline-flex h-9 items-center gap-2 rounded-lg border border-border px-3 text-xs font-semibold hover:bg-muted"
                            >
                              <FileText className="size-3.5" />
                              View receipt
                            </button>
                          ) : (
                            <button
                              type="button"
                              disabled={generatingReceiptId === payment.id}
                              onClick={() =>
                                void handleGenerateReceipt(payment)
                              }
                              className="inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3 text-xs font-semibold text-primary-foreground disabled:opacity-50"
                            >
                              {generatingReceiptId === payment.id ? (
                                <LoaderCircle className="size-3.5 animate-spin" />
                              ) : (
                                <ReceiptText className="size-3.5" />
                              )}
                              Generate
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          ) : null}
        </section>
      </div>

      {selectedInvoice && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/35 sm:items-center sm:p-6">
          <button
            type="button"
            onClick={closePayment}
            className="absolute inset-0 cursor-default"
            aria-label="Close payment form"
          />

          <div className="relative z-10 w-full max-w-xl rounded-t-3xl border border-border bg-card shadow-2xl sm:rounded-3xl">
            <div className="flex items-start justify-between border-b border-border px-5 py-4 sm:px-6">
              <div>
                <p className="text-xs font-semibold text-primary">
                  RECORD PAYMENT
                </p>
                <h3 className="mt-1 text-lg font-semibold">
                  {selectedInvoice.invoiceNumber}
                </h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  {selectedInvoice.customerName} ·{" "}
                  {formatMoney(
                    selectedInvoice.balanceDue,
                    selectedInvoice.currencyCode,
                  )}{" "}
                  remaining
                </p>
              </div>
              <button
                type="button"
                onClick={closePayment}
                className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted"
              >
                <X className="size-4.5" />
              </button>
            </div>

            <form onSubmit={handlePayment} className="space-y-5 p-5 sm:p-6">
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium">Amount</span>
                  <input
                    type="number"
                    min="0.01"
                    step="0.01"
                    max={selectedInvoice.balanceDue}
                    value={amount}
                    onChange={(event) => setAmount(event.target.value)}
                    className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none focus:border-primary"
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium">
                    Payment method
                  </span>
                  <select
                    value={method}
                    onChange={(event) => setMethod(Number(event.target.value))}
                    className="h-11 w-full rounded-xl border border-input bg-background px-3 text-sm outline-none focus:border-primary"
                  >
                    {Object.entries(methodLabels).map(([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <label className="block">
                <span className="mb-2 block text-sm font-medium">
                  Payment date
                </span>
                <input
                  type="date"
                  max={today()}
                  value={paymentDate}
                  onChange={(event) => setPaymentDate(event.target.value)}
                  className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none focus:border-primary"
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-medium">
                  Reference
                </span>
                <input
                  value={reference}
                  onChange={(event) => setReference(event.target.value)}
                  placeholder="Bank reference, EFT reference, etc."
                  className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-medium">Notes</span>
                <textarea
                  rows={3}
                  value={notes}
                  onChange={(event) => setNotes(event.target.value)}
                  placeholder="Optional payment notes"
                  className="w-full resize-none rounded-xl border border-input bg-background px-4 py-3 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                />
              </label>

              {formError && (
                <div
                  className="rounded-xl px-4 py-3 text-sm"
                  style={{
                    color: "var(--status-danger)",
                    backgroundColor: "var(--status-danger-bg)",
                  }}
                >
                  {formError}
                </div>
              )}

              <div className="flex flex-col-reverse gap-3 border-t border-border pt-5 sm:flex-row sm:justify-end">
                <button
                  type="button"
                  onClick={closePayment}
                  disabled={isSaving}
                  className="h-10 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-muted disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-60"
                >
                  {isSaving && <LoaderCircle className="size-4 animate-spin" />}
                  Record payment
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
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
