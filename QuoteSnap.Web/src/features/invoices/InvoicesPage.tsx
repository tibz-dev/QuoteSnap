import { useEffect, useMemo, useState } from "react"
import {
  ArrowRight,
  CalendarDays,
  CheckCircle2,
  Download,
  Eye,
  FileCheck2,
  LoaderCircle,
  Mail,
  Send,
  Search,
  X,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { quoteApi } from "@/features/quotes/quote.api"
import { QuoteStatus, type Quote } from "@/features/quotes/quote.types"
import { invoiceApi } from "./invoice.api"
import { InvoiceStatus, type Invoice } from "./invoice.types"

const invoiceStatusStyles: Record<
  number,
  { label: string; color: string; background: string }
> = {
  [InvoiceStatus.Draft]: {
    label: "Draft",
    color: "var(--status-draft)",
    background: "var(--status-draft-bg)",
  },
  [InvoiceStatus.Sent]: {
    label: "Sent",
    color: "var(--status-sent)",
    background: "var(--status-sent-bg)",
  },
  [InvoiceStatus.PartiallyPaid]: {
    label: "Partially paid",
    color: "var(--status-warning)",
    background: "var(--status-warning-bg)",
  },
  [InvoiceStatus.Paid]: {
    label: "Paid",
    color: "var(--status-success)",
    background: "var(--status-success-bg)",
  },
  [InvoiceStatus.Overdue]: {
    label: "Overdue",
    color: "var(--status-danger)",
    background: "var(--status-danger-bg)",
  },
  [InvoiceStatus.Cancelled]: {
    label: "Cancelled",
    color: "var(--status-draft)",
    background: "var(--status-draft-bg)",
  },
}

function defaultDueDate() {
  const date = new Date()
  date.setDate(date.getDate() + 30)
  return date.toISOString().slice(0, 10)
}

type InvoicesPageProps = {
  createSignal?: number
  onNavigate?: (destination: string) => void
}

export function InvoicesPage({
  createSignal = 0,
  onNavigate,
}: InvoicesPageProps) {
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [quotes, setQuotes] = useState<Quote[]>([])
  const [search, setSearch] = useState("")
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState("")
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null)
  const [convertingQuote, setConvertingQuote] = useState<Quote | null>(null)
  const [dueDate, setDueDate] = useState(defaultDueDate())
  const [isConverting, setIsConverting] = useState(false)
  const [convertError, setConvertError] = useState("")
  const [isCreateOpen, setIsCreateOpen] = useState(false)

  useEffect(() => {
    void loadData()
  }, [])

  useEffect(() => {
    if (createSignal > 0 && !isLoading) {
      setIsCreateOpen(true)
    }
  }, [createSignal, isLoading])

  async function loadData() {
    try {
      setIsLoading(true)
      setError("")
      const [invoiceRows, quoteRows] = await Promise.all([
        invoiceApi.getAll(),
        quoteApi.getAll(),
      ])
      setInvoices(invoiceRows)
      setQuotes(quoteRows)
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your invoices.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const acceptedQuotes = useMemo(
    () => quotes.filter((quote) => quote.status === QuoteStatus.Accepted),
    [quotes],
  )

  const filteredInvoices = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return invoices

    return invoices.filter((invoice) =>
      [invoice.invoiceNumber, invoice.customerName]
        .some((value) => value.toLowerCase().includes(query)),
    )
  }, [invoices, search])

  const openConversion = (quote: Quote) => {
    setIsCreateOpen(false)
    setConvertingQuote(quote)
    setDueDate(defaultDueDate())
    setConvertError("")
  }

  const closeConversion = () => {
    if (!isConverting) {
      setConvertingQuote(null)
      setConvertError("")
    }
  }

  const handleConvert = async () => {
    if (!convertingQuote) return

    const due = dueDate
      ? new Date(`${dueDate}T23:59:59`).toISOString()
      : null

    try {
      setIsConverting(true)
      setConvertError("")

      const invoice = await invoiceApi.convertQuote(convertingQuote.id, {
        dueDate: due,
      })

      setInvoices((current) => [invoice, ...current])
      setQuotes((current) =>
        current.map((quote) =>
          quote.id === convertingQuote.id
            ? { ...quote, status: QuoteStatus.ConvertedToInvoice }
            : quote,
        ),
      )
      setConvertingQuote(null)
      setSelectedInvoice(invoice)
    } catch (error) {
      setConvertError(
        error instanceof ApiError
          ? error.message
          : "We couldn't convert this quote to an invoice.",
      )
    } finally {
      setIsConverting(false)
    }
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-sm font-semibold text-primary">INVOICES</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
              Invoices & balances
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Convert accepted quotes and track what has been paid or is still due.
            </p>
          </div>

          <button
            type="button"
            onClick={() => setIsCreateOpen(true)}
            className="inline-flex h-10 items-center justify-center gap-2 self-start rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground shadow-sm transition hover:opacity-95"
          >
            <FileCheck2 className="size-4" />
            Create invoice
          </button>
        </div>

        {acceptedQuotes.length > 0 && (
          <section className="mt-8 rounded-2xl border border-border bg-accent/55 p-4 sm:p-5">
            <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
              <div>
                <p className="text-sm font-semibold text-accent-foreground">
                  {acceptedQuotes.length} accepted{" "}
                  {acceptedQuotes.length === 1 ? "quote is" : "quotes are"} ready
                  to invoice
                </p>
                <p className="mt-1 text-xs text-muted-foreground">
                  Conversion keeps the customer, line items, tax, notes and terms.
                </p>
              </div>
            </div>

            <div className="mt-4 grid gap-3 lg:grid-cols-2">
              {acceptedQuotes.map((quote) => (
                <article
                  key={quote.id}
                  className="flex flex-col justify-between gap-4 rounded-xl border border-border bg-card p-4 sm:flex-row sm:items-center"
                >
                  <div>
                    <p className="text-sm font-semibold">{quote.quoteNumber}</p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {quote.customerName} · {formatMoney(quote.total, quote.currencyCode)}
                    </p>
                  </div>

                  <button
                    type="button"
                    onClick={() => openConversion(quote)}
                    className="inline-flex h-9 items-center justify-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
                  >
                    Convert to invoice
                    <ArrowRight className="size-3.5" />
                  </button>
                </article>
              ))}
            </div>
          </section>
        )}

        <div className="mt-6 rounded-2xl border border-border bg-card">
          <div className="flex flex-col gap-4 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative w-full sm:max-w-sm">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search invoice or customer..."
                className="h-10 w-full rounded-xl border border-input bg-background pl-10 pr-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
              />
            </div>
            <p className="text-xs text-muted-foreground">
              {invoices.length} {invoices.length === 1 ? "invoice" : "invoices"}
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

          {isLoading ? (
            <div className="flex min-h-72 items-center justify-center">
              <LoaderCircle className="size-6 animate-spin text-primary" />
            </div>
          ) : filteredInvoices.length === 0 ? (
            <div className="flex min-h-72 flex-col items-center justify-center px-6 py-12 text-center">
              <span className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
                <FileCheck2 className="size-5" />
              </span>
              <p className="mt-4 text-sm font-semibold">
                {search ? "No matching invoices" : "No invoices yet"}
              </p>
              <p className="mt-1 max-w-sm text-xs leading-5 text-muted-foreground">
                Accept a quote first, then convert it here into an invoice.
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[980px] text-left">
                <thead>
                  <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                    <th className="px-5 py-3 font-medium">Invoice</th>
                    <th className="px-5 py-3 font-medium">Customer</th>
                    <th className="px-5 py-3 font-medium">Status</th>
                    <th className="px-5 py-3 font-medium">Due date</th>
                    <th className="px-5 py-3 text-right font-medium">Total</th>
                    <th className="px-5 py-3 text-right font-medium">Paid</th>
                    <th className="px-5 py-3 text-right font-medium">Balance</th>
                    <th className="px-5 py-3 text-right font-medium">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredInvoices.map((invoice) => {
                    const status =
                      invoiceStatusStyles[invoice.status] ||
                      invoiceStatusStyles[InvoiceStatus.Draft]

                    return (
                      <tr
                        key={invoice.id}
                        className="border-b border-border last:border-0 hover:bg-muted/30"
                      >
                        <td className="px-5 py-4">
                          <p className="text-sm font-semibold">
                            {invoice.invoiceNumber}
                          </p>
                          <p className="mt-1 text-xs text-muted-foreground">
                            {formatDate(invoice.issueDate)}
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
                        <td className="px-5 py-4 text-xs text-muted-foreground">
                          {formatDate(invoice.dueDate)}
                        </td>
                        <td className="px-5 py-4 text-right text-sm font-semibold">
                          {formatMoney(invoice.total, invoice.currencyCode)}
                        </td>
                        <td className="px-5 py-4 text-right text-sm text-muted-foreground">
                          {formatMoney(invoice.amountPaid, invoice.currencyCode)}
                        </td>
                        <td className="px-5 py-4 text-right text-sm font-semibold">
                          {formatMoney(invoice.balanceDue, invoice.currencyCode)}
                        </td>
                        <td className="px-5 py-4 text-right">
                          <button
                            type="button"
                            onClick={() => setSelectedInvoice(invoice)}
                            className="inline-flex h-9 items-center gap-2 rounded-lg border border-border px-3 text-xs font-semibold transition hover:bg-muted"
                          >
                            <Eye className="size-3.5" />
                            View
                          </button>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {isCreateOpen && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/35 sm:items-center sm:p-6">
          <button
            type="button"
            onClick={() => setIsCreateOpen(false)}
            className="absolute inset-0 cursor-default"
            aria-label="Close create invoice"
          />

          <div className="relative z-10 max-h-[90vh] w-full max-w-xl overflow-y-auto rounded-t-3xl border border-border bg-card p-5 shadow-2xl sm:rounded-3xl sm:p-6">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-xs font-semibold text-primary">NEW INVOICE</p>
                <h3 className="mt-1 text-xl font-semibold">
                  Choose an accepted quote
                </h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">
                  QuoteSnap creates invoices from accepted quotes so the customer,
                  line items, tax and terms remain consistent.
                </p>
              </div>
              <button
                type="button"
                onClick={() => setIsCreateOpen(false)}
                className="inline-flex size-9 shrink-0 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted"
              >
                <X className="size-4.5" />
              </button>
            </div>

            {acceptedQuotes.length === 0 ? (
              <div className="mt-6 rounded-2xl border border-dashed border-border bg-background p-6 text-center">
                <FileCheck2 className="mx-auto size-7 text-muted-foreground" />
                <p className="mt-3 text-sm font-semibold">
                  No accepted quotes are ready
                </p>
                <p className="mt-2 text-xs leading-5 text-muted-foreground">
                  Create a quote and mark it Accepted before converting it into an invoice.
                </p>
                <button
                  type="button"
                  onClick={() => {
                    setIsCreateOpen(false)
                    onNavigate?.("Quotes")
                  }}
                  className="mt-5 inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
                >
                  Go to quotes
                  <ArrowRight className="size-3.5" />
                </button>
              </div>
            ) : (
              <div className="mt-6 space-y-3">
                {acceptedQuotes.map((quote) => (
                  <button
                    key={quote.id}
                    type="button"
                    onClick={() => openConversion(quote)}
                    className="flex w-full items-center justify-between gap-4 rounded-2xl border border-border bg-background p-4 text-left transition hover:bg-muted"
                  >
                    <div>
                      <p className="text-sm font-semibold">{quote.quoteNumber}</p>
                      <p className="mt-1 text-xs text-muted-foreground">
                        {quote.customerName}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-semibold">
                        {formatMoney(quote.total, quote.currencyCode)}
                      </p>
                      <p className="mt-1 flex items-center justify-end gap-1 text-xs font-semibold text-primary">
                        Convert
                        <ArrowRight className="size-3.5" />
                      </p>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {convertingQuote && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/35 sm:items-center sm:p-6">
          <button
            type="button"
            onClick={closeConversion}
            className="absolute inset-0 cursor-default"
            aria-label="Close conversion"
          />

          <div className="relative z-10 w-full max-w-lg rounded-t-3xl border border-border bg-card p-5 shadow-2xl sm:rounded-3xl sm:p-6">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-xs font-semibold text-primary">CONVERT QUOTE</p>
                <h3 className="mt-2 text-xl font-semibold">
                  {convertingQuote.quoteNumber}
                </h3>
                <p className="mt-1 text-sm text-muted-foreground">
                  {convertingQuote.customerName} ·{" "}
                  {formatMoney(
                    convertingQuote.total,
                    convertingQuote.currencyCode,
                  )}
                </p>
              </div>
              <button
                type="button"
                onClick={closeConversion}
                className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted"
              >
                <X className="size-4.5" />
              </button>
            </div>

            <div className="mt-6 rounded-xl bg-accent p-4 text-xs leading-5 text-accent-foreground">
              QuoteSnap will copy every quote line into the invoice and mark this
              quote as converted. This action can only happen once.
            </div>

            <label className="mt-5 block">
              <span className="mb-2 block text-sm font-medium">Invoice due date</span>
              <div className="relative">
                <CalendarDays className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  type="date"
                  value={dueDate}
                  min={new Date().toISOString().slice(0, 10)}
                  onChange={(event) => setDueDate(event.target.value)}
                  className="h-11 w-full rounded-xl border border-input bg-background pl-10 pr-3 text-sm outline-none focus:border-primary"
                />
              </div>
            </label>

            {convertError && (
              <div
                className="mt-4 rounded-xl px-4 py-3 text-sm"
                style={{
                  color: "var(--status-danger)",
                  backgroundColor: "var(--status-danger-bg)",
                }}
              >
                {convertError}
              </div>
            )}

            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={closeConversion}
                disabled={isConverting}
                className="h-10 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-muted disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => void handleConvert()}
                disabled={isConverting}
                className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-60"
              >
                {isConverting ? (
                  <LoaderCircle className="size-4 animate-spin" />
                ) : (
                  <FileCheck2 className="size-4" />
                )}
                Create invoice
              </button>
            </div>
          </div>
        </div>
      )}

      {selectedInvoice && (
        <InvoiceDetail
          invoice={selectedInvoice}
          onClose={() => setSelectedInvoice(null)}
        />
      )}
    </section>
  )
}

function InvoiceDetail({
  invoice,
  onClose,
}: {
  invoice: Invoice
  onClose: () => void
}) {
  const [isDownloading, setIsDownloading] = useState(false)
  const [isEmailOpen, setIsEmailOpen] = useState(false)
  const [isSending, setIsSending] = useState(false)
  const [emailTo, setEmailTo] = useState("")
  const [emailSubject, setEmailSubject] = useState("")
  const [emailMessage, setEmailMessage] = useState("")
  const [deliveryError, setDeliveryError] = useState("")
  const [deliverySuccess, setDeliverySuccess] = useState("")

  const status =
    invoiceStatusStyles[invoice.status] ||
    invoiceStatusStyles[InvoiceStatus.Draft]

  const handleDownload = async () => {
    try {
      setIsDownloading(true)
      setDeliveryError("")
      setDeliverySuccess("")

      const { blob, fileName } = await invoiceApi.downloadPdf(invoice.id)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement("a")
      anchor.href = url
      anchor.download = fileName
      document.body.appendChild(anchor)
      anchor.click()
      anchor.remove()
      URL.revokeObjectURL(url)
    } catch (error) {
      setDeliveryError(
        error instanceof ApiError
          ? error.message
          : "We couldn't download this invoice.",
      )
    } finally {
      setIsDownloading(false)
    }
  }

  const handleSendEmail = async () => {
    try {
      setIsSending(true)
      setDeliveryError("")
      setDeliverySuccess("")

      const result = await invoiceApi.sendEmail(invoice.id, {
        to: emailTo.trim() || null,
        subject: emailSubject.trim() || null,
        message: emailMessage.trim() || null,
      })

      setDeliverySuccess(result.message || "Invoice email sent successfully.")
      setIsEmailOpen(false)
      setEmailTo("")
      setEmailSubject("")
      setEmailMessage("")
    } catch (error) {
      setDeliveryError(
        error instanceof ApiError
          ? error.message
          : "We couldn't send this invoice email.",
      )
    } finally {
      setIsSending(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/35 sm:items-center sm:p-6">
      <button
        type="button"
        onClick={onClose}
        className="absolute inset-0 cursor-default"
        aria-label="Close invoice"
      />

      <div className="relative z-10 max-h-[94vh] w-full max-w-3xl overflow-y-auto rounded-t-3xl border border-border bg-card shadow-2xl sm:rounded-3xl">
        <div className="sticky top-0 z-10 flex items-start justify-between border-b border-border bg-card px-5 py-4 sm:px-6">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-xl font-semibold">{invoice.invoiceNumber}</h3>
              <span
                className="rounded-full px-2.5 py-1 text-xs font-semibold"
                style={{
                  color: status.color,
                  backgroundColor: status.background,
                }}
              >
                {status.label}
              </span>
            </div>
            <p className="mt-1 text-sm text-muted-foreground">
              {invoice.customerName}
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted"
          >
            <X className="size-4.5" />
          </button>
        </div>

        <div className="space-y-6 p-5 sm:p-6">
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              disabled={isDownloading}
              onClick={() => void handleDownload()}
              className="inline-flex h-10 items-center gap-2 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-muted disabled:opacity-50"
            >
              {isDownloading ? (
                <LoaderCircle className="size-4 animate-spin" />
              ) : (
                <Download className="size-4" />
              )}
              Download PDF
            </button>

            <button
              type="button"
              onClick={() => {
                setDeliveryError("")
                setDeliverySuccess("")
                setIsEmailOpen(true)
              }}
              className="inline-flex h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground"
            >
              <Mail className="size-4" />
              Send email
            </button>
          </div>

          {deliverySuccess && (
            <div
              className="rounded-xl px-4 py-3 text-sm"
              style={{
                color: "var(--status-success)",
                backgroundColor: "var(--status-success-bg)",
              }}
            >
              {deliverySuccess}
            </div>
          )}

          {deliveryError && (
            <div
              className="rounded-xl px-4 py-3 text-sm"
              style={{
                color: "var(--status-danger)",
                backgroundColor: "var(--status-danger-bg)",
              }}
            >
              {deliveryError}
            </div>
          )}

          <div className="grid gap-3 sm:grid-cols-4">
            <Metric label="Issue date" value={formatDate(invoice.issueDate)} />
            <Metric label="Due date" value={formatDate(invoice.dueDate)} />
            <Metric
              label="Amount paid"
              value={formatMoney(invoice.amountPaid, invoice.currencyCode)}
            />
            <Metric
              label="Balance due"
              value={formatMoney(invoice.balanceDue, invoice.currencyCode)}
              emphasize
            />
          </div>

          <div className="overflow-x-auto rounded-2xl border border-border">
            <table className="w-full min-w-[620px] text-left">
              <thead>
                <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                  <th className="px-4 py-3 font-medium">Description</th>
                  <th className="px-4 py-3 text-right font-medium">Qty</th>
                  <th className="px-4 py-3 text-right font-medium">Price</th>
                  <th className="px-4 py-3 text-right font-medium">Discount</th>
                  <th className="px-4 py-3 text-right font-medium">Total</th>
                </tr>
              </thead>
              <tbody>
                {invoice.items.map((item) => (
                  <tr
                    key={item.id}
                    className="border-b border-border last:border-0"
                  >
                    <td className="px-4 py-3 text-sm">{item.description}</td>
                    <td className="px-4 py-3 text-right text-sm">
                      {item.quantity}
                    </td>
                    <td className="px-4 py-3 text-right text-sm">
                      {formatMoney(item.unitPrice, invoice.currencyCode)}
                    </td>
                    <td className="px-4 py-3 text-right text-sm text-muted-foreground">
                      {formatMoney(item.discountAmount, invoice.currencyCode)}
                    </td>
                    <td className="px-4 py-3 text-right text-sm font-semibold">
                      {formatMoney(item.lineTotal, invoice.currencyCode)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="ml-auto w-full max-w-sm space-y-2 rounded-2xl bg-muted p-4 text-sm">
            <SummaryRow
              label="Subtotal"
              value={formatMoney(invoice.subtotal, invoice.currencyCode)}
            />
            <SummaryRow
              label="Discount"
              value={formatMoney(invoice.discountAmount, invoice.currencyCode)}
            />
            {invoice.taxRate > 0 && (
              <SummaryRow
                label={invoice.taxName || `Tax (${invoice.taxRate}%)`}
                value={formatMoney(invoice.taxAmount, invoice.currencyCode)}
              />
            )}
            <div className="my-2 border-t border-border" />
            <SummaryRow
              label="Invoice total"
              value={formatMoney(invoice.total, invoice.currencyCode)}
              strong
            />
            <SummaryRow
              label="Balance due"
              value={formatMoney(invoice.balanceDue, invoice.currencyCode)}
              strong
            />
          </div>

          {(invoice.notes || invoice.terms) && (
            <div className="grid gap-4 sm:grid-cols-2">
              {invoice.notes && (
                <div className="rounded-xl border border-border p-4">
                  <p className="text-xs font-semibold text-muted-foreground">
                    NOTES
                  </p>
                  <p className="mt-2 text-sm leading-6">{invoice.notes}</p>
                </div>
              )}
              {invoice.terms && (
                <div className="rounded-xl border border-border p-4">
                  <p className="text-xs font-semibold text-muted-foreground">
                    TERMS
                  </p>
                  <p className="mt-2 text-sm leading-6">{invoice.terms}</p>
                </div>
              )}
            </div>
          )}

          {invoice.quoteId && (
            <div className="flex items-center gap-2 rounded-xl bg-accent p-3 text-xs text-accent-foreground">
              <CheckCircle2 className="size-4" />
              Created from an accepted QuoteSnap quote.
            </div>
          )}
        </div>
      </div>

      {isEmailOpen && (
        <div className="absolute inset-0 z-20 flex items-end justify-center bg-black/35 sm:items-center sm:p-6">
          <button
            type="button"
            onClick={() => !isSending && setIsEmailOpen(false)}
            className="absolute inset-0 cursor-default"
            aria-label="Close email form"
          />

          <div className="relative z-10 w-full max-w-lg rounded-t-3xl border border-border bg-card p-5 shadow-2xl sm:rounded-3xl sm:p-6">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-xs font-semibold text-primary">SEND INVOICE</p>
                <h4 className="mt-1 text-lg font-semibold">
                  {invoice.invoiceNumber}
                </h4>
              </div>
              <button
                type="button"
                onClick={() => setIsEmailOpen(false)}
                disabled={isSending}
                className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted disabled:opacity-50"
              >
                <X className="size-4.5" />
              </button>
            </div>

            <div className="mt-5 space-y-4">
              <label className="block">
                <span className="mb-2 block text-sm font-medium">To</span>
                <input
                  type="email"
                  value={emailTo}
                  onChange={(event) => setEmailTo(event.target.value)}
                  placeholder="Leave blank to use the customer's email"
                  className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-medium">Subject</span>
                <input
                  value={emailSubject}
                  onChange={(event) => setEmailSubject(event.target.value)}
                  placeholder="Use QuoteSnap default subject"
                  className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-medium">Message</span>
                <textarea
                  rows={4}
                  value={emailMessage}
                  onChange={(event) => setEmailMessage(event.target.value)}
                  placeholder="Optional message to the customer"
                  className="w-full resize-none rounded-xl border border-input bg-background px-4 py-3 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                />
              </label>

              <p className="text-xs leading-5 text-muted-foreground">
                QuoteSnap generates the invoice PDF and attaches it automatically.
              </p>
            </div>

            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => setIsEmailOpen(false)}
                disabled={isSending}
                className="h-10 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-muted disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => void handleSendEmail()}
                disabled={isSending}
                className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-60"
              >
                {isSending ? (
                  <LoaderCircle className="size-4 animate-spin" />
                ) : (
                  <Send className="size-4" />
                )}
                Send invoice
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function Metric({
  label,
  value,
  emphasize = false,
}: {
  label: string
  value: string
  emphasize?: boolean
}) {
  return (
    <div className="rounded-xl border border-border bg-background p-3">
      <p className="text-[11px] text-muted-foreground">{label}</p>
      <p
        className={`mt-1 text-sm font-semibold ${
          emphasize ? "text-primary" : ""
        }`}
      >
        {value}
      </p>
    </div>
  )
}

function SummaryRow({
  label,
  value,
  strong = false,
}: {
  label: string
  value: string
  strong?: boolean
}) {
  return (
    <div className="flex items-center justify-between gap-4">
      <span className={strong ? "font-semibold" : "text-muted-foreground"}>
        {label}
      </span>
      <span className={strong ? "font-semibold" : ""}>{value}</span>
    </div>
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
