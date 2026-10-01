import { useEffect, useMemo, useState, type FormEvent } from "react"
import {
  CalendarDays,
  FileText,
  LoaderCircle,
  Plus,
  Search,
  Trash2,
  X,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { catalogueApi } from "@/features/catalogue/catalogue.api"
import type { CatalogueItem } from "@/features/catalogue/catalogue.types"
import { customerApi } from "@/features/customers/customer.api"
import type { Customer } from "@/features/customers/customer.types"
import { quoteApi } from "./quote.api"
import { QuoteStatus, type CreateQuoteInput, type Quote } from "./quote.types"

type DraftLine = {
  key: string
  catalogueItemId: string
  description: string
  quantity: string
  unitPrice: string
  discountAmount: string
}

const statusStyles: Record<
  number,
  { label: string; color: string; background: string }
> = {
  [QuoteStatus.Draft]: {
    label: "Draft",
    color: "var(--status-draft)",
    background: "var(--status-draft-bg)",
  },
  [QuoteStatus.Sent]: {
    label: "Sent",
    color: "var(--status-sent)",
    background: "var(--status-sent-bg)",
  },
  [QuoteStatus.Viewed]: {
    label: "Viewed",
    color: "var(--status-viewed)",
    background: "var(--status-viewed-bg)",
  },
  [QuoteStatus.Accepted]: {
    label: "Accepted",
    color: "var(--status-success)",
    background: "var(--status-success-bg)",
  },
  [QuoteStatus.Declined]: {
    label: "Declined",
    color: "var(--status-danger)",
    background: "var(--status-danger-bg)",
  },
  [QuoteStatus.Expired]: {
    label: "Expired",
    color: "var(--status-danger)",
    background: "var(--status-danger-bg)",
  },
  [QuoteStatus.ConvertedToInvoice]: {
    label: "Invoiced",
    color: "var(--status-success)",
    background: "var(--status-success-bg)",
  },
  [QuoteStatus.Cancelled]: {
    label: "Cancelled",
    color: "var(--status-draft)",
    background: "var(--status-draft-bg)",
  },
}

function makeLine(): DraftLine {
  return {
    key: crypto.randomUUID(),
    catalogueItemId: "",
    description: "",
    quantity: "1",
    unitPrice: "",
    discountAmount: "0",
  }
}

function defaultValidUntil() {
  const date = new Date()
  date.setDate(date.getDate() + 7)
  return date.toISOString().slice(0, 10)
}

export function QuotesPage() {
  const [quotes, setQuotes] = useState<Quote[]>([])
  const [customers, setCustomers] = useState<Customer[]>([])
  const [catalogue, setCatalogue] = useState<CatalogueItem[]>([])
  const [search, setSearch] = useState("")
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [formError, setFormError] = useState("")
  const [customerId, setCustomerId] = useState("")
  const [validUntil, setValidUntil] = useState(defaultValidUntil())
  const [taxRate, setTaxRate] = useState("")
  const [notes, setNotes] = useState("")
  const [terms, setTerms] = useState("")
  const [lines, setLines] = useState<DraftLine[]>([makeLine()])

  useEffect(() => {
    void loadData()
  }, [])

  async function loadData() {
    try {
      setIsLoading(true)
      setError("")
      const [quoteRows, customerRows, catalogueRows] = await Promise.all([
        quoteApi.getAll(),
        customerApi.getAll(),
        catalogueApi.getAll(),
      ])
      setQuotes(quoteRows)
      setCustomers(customerRows)
      setCatalogue(catalogueRows.filter((item) => item.isActive))
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your quotes.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const filteredQuotes = useMemo(() => {
    const query = search.trim().toLowerCase()
    if (!query) return quotes

    return quotes.filter((quote) =>
      [quote.quoteNumber, quote.customerName]
        .some((value) => value.toLowerCase().includes(query)),
    )
  }, [quotes, search])

  const openCreate = () => {
    setCustomerId("")
    setValidUntil(defaultValidUntil())
    setTaxRate("")
    setNotes("")
    setTerms("")
    setLines([makeLine()])
    setFormError("")
    setIsFormOpen(true)
  }

  const closeForm = () => {
    if (!isSaving) setIsFormOpen(false)
  }

  const updateLine = (
    key: string,
    field: keyof Omit<DraftLine, "key">,
    value: string,
  ) => {
    setLines((current) =>
      current.map((line) =>
        line.key === key ? { ...line, [field]: value } : line,
      ),
    )
  }

  const selectCatalogueItem = (key: string, itemId: string) => {
    const item = catalogue.find((entry) => entry.id === itemId)

    setLines((current) =>
      current.map((line) =>
        line.key === key
          ? {
              ...line,
              catalogueItemId: itemId,
              description: item?.name || "",
              unitPrice:
                item?.defaultPrice === null || item?.defaultPrice === undefined
                  ? ""
                  : String(item.defaultPrice),
            }
          : line,
      ),
    )
  }

  const removeLine = (key: string) => {
    setLines((current) =>
      current.length === 1
        ? current
        : current.filter((line) => line.key !== key),
    )
  }

  const estimatedSubtotal = useMemo(
    () =>
      lines.reduce((total, line) => {
        const quantity = Number(line.quantity) || 0
        const unitPrice = Number(line.unitPrice) || 0
        const discount = Number(line.discountAmount) || 0
        return total + Math.max(quantity * unitPrice - discount, 0)
      }, 0),
    [lines],
  )

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFormError("")

    if (!customerId) {
      setFormError("Select a customer.")
      return
    }

    for (const line of lines) {
      const quantity = Number(line.quantity)
      const price = line.unitPrice === "" ? null : Number(line.unitPrice)
      const discount = Number(line.discountAmount || "0")

      if (quantity <= 0) {
        setFormError("Every quote item must have a quantity greater than zero.")
        return
      }

      if (!line.catalogueItemId && !line.description.trim()) {
        setFormError("Custom quote items need a description.")
        return
      }

      if (price === null || Number.isNaN(price) || price < 0) {
        setFormError("Every quote item needs a valid unit price.")
        return
      }

      if (discount < 0 || discount > quantity * price) {
        setFormError("Line discount cannot exceed the line subtotal.")
        return
      }
    }

    const request: CreateQuoteInput = {
      customerId,
      validUntil: validUntil
        ? new Date(`${validUntil}T23:59:59`).toISOString()
        : null,
      currencyCode: "ZAR",
      taxRate: taxRate === "" ? null : Number(taxRate),
      notes: notes.trim() || null,
      terms: terms.trim() || null,
      items: lines.map((line) => ({
        catalogueItemId: line.catalogueItemId || null,
        description: line.description.trim() || null,
        quantity: Number(line.quantity),
        unitPrice: line.unitPrice === "" ? null : Number(line.unitPrice),
        discountAmount: Number(line.discountAmount || "0"),
      })),
    }

    try {
      setIsSaving(true)
      const quote = await quoteApi.create(request)
      setQuotes((current) => [quote, ...current])
      setIsFormOpen(false)
    } catch (error) {
      setFormError(
        error instanceof ApiError
          ? error.message
          : "We couldn't create this quote.",
      )
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-sm font-semibold text-primary">QUOTES</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
              Quotes & estimates
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Build professional quotes from saved customers and catalogue items.
            </p>
          </div>

          <button
            type="button"
            onClick={openCreate}
            className="inline-flex h-10 items-center justify-center gap-2 self-start rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground shadow-sm transition hover:opacity-95"
          >
            <Plus className="size-4" />
            Create quote
          </button>
        </div>

        <div className="mt-8 rounded-2xl border border-border bg-card">
          <div className="flex flex-col gap-4 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative w-full sm:max-w-sm">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search quote or customer..."
                className="h-10 w-full rounded-xl border border-input bg-background pl-10 pr-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
              />
            </div>
            <p className="text-xs text-muted-foreground">
              {quotes.length} {quotes.length === 1 ? "quote" : "quotes"}
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
          ) : filteredQuotes.length === 0 ? (
            <div className="flex min-h-72 flex-col items-center justify-center px-6 py-12 text-center">
              <span className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
                <FileText className="size-5" />
              </span>
              <p className="mt-4 text-sm font-semibold">
                {search ? "No matching quotes" : "No quotes yet"}
              </p>
              <p className="mt-1 max-w-sm text-xs leading-5 text-muted-foreground">
                {customers.length === 0
                  ? "Add a customer first, then come back to create a quote."
                  : "Create your first quote using a customer and one or more services."}
              </p>
              {customers.length > 0 && !search && (
                <button
                  type="button"
                  onClick={openCreate}
                  className="mt-5 inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
                >
                  <Plus className="size-3.5" />
                  Create quote
                </button>
              )}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[860px] text-left">
                <thead>
                  <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                    <th className="px-5 py-3 font-medium">Quote</th>
                    <th className="px-5 py-3 font-medium">Customer</th>
                    <th className="px-5 py-3 font-medium">Status</th>
                    <th className="px-5 py-3 font-medium">Valid until</th>
                    <th className="px-5 py-3 text-right font-medium">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredQuotes.map((quote) => {
                    const status =
                      statusStyles[quote.status] || statusStyles[QuoteStatus.Draft]

                    return (
                      <tr
                        key={quote.id}
                        className="border-b border-border last:border-0 hover:bg-muted/30"
                      >
                        <td className="px-5 py-4">
                          <p className="text-sm font-semibold">{quote.quoteNumber}</p>
                          <p className="mt-1 text-xs text-muted-foreground">
                            {quote.items.length} {quote.items.length === 1 ? "item" : "items"}
                          </p>
                        </td>
                        <td className="px-5 py-4 text-sm">{quote.customerName}</td>
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
                          {formatDate(quote.validUntil)}
                        </td>
                        <td className="px-5 py-4 text-right text-sm font-semibold">
                          {formatMoney(quote.total, quote.currencyCode)}
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

      {isFormOpen && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/35 sm:items-center sm:p-6">
          <button
            type="button"
            onClick={closeForm}
            className="absolute inset-0 cursor-default"
            aria-label="Close quote form"
          />

          <div className="relative z-10 max-h-[95vh] w-full max-w-4xl overflow-y-auto rounded-t-3xl border border-border bg-card shadow-2xl sm:rounded-3xl">
            <div className="sticky top-0 z-20 flex items-start justify-between border-b border-border bg-card px-5 py-4 sm:px-6">
              <div>
                <h3 className="text-lg font-semibold">Create quote</h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  Select a customer, add line items, then QuoteSnap calculates the final totals.
                </p>
              </div>
              <button
                type="button"
                onClick={closeForm}
                className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted"
              >
                <X className="size-4.5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6 p-5 sm:p-6">
              {customers.length === 0 ? (
                <div
                  className="rounded-xl px-4 py-3 text-sm"
                  style={{
                    color: "var(--status-warning)",
                    backgroundColor: "var(--status-warning-bg)",
                  }}
                >
                  You need at least one customer before creating a quote.
                </div>
              ) : (
                <>
                  <div className="grid gap-4 sm:grid-cols-2">
                    <label className="block">
                      <span className="mb-2 block text-sm font-medium">Customer</span>
                      <select
                        value={customerId}
                        onChange={(event) => setCustomerId(event.target.value)}
                        className="h-11 w-full rounded-xl border border-input bg-background px-3 text-sm outline-none focus:border-primary"
                      >
                        <option value="">Select customer</option>
                        {customers.map((customer) => (
                          <option key={customer.id} value={customer.id}>
                            {customer.name}
                            {customer.companyName ? ` — ${customer.companyName}` : ""}
                          </option>
                        ))}
                      </select>
                    </label>

                    <label className="block">
                      <span className="mb-2 block text-sm font-medium">Valid until</span>
                      <div className="relative">
                        <CalendarDays className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                        <input
                          type="date"
                          value={validUntil}
                          onChange={(event) => setValidUntil(event.target.value)}
                          className="h-11 w-full rounded-xl border border-input bg-background pl-10 pr-3 text-sm outline-none focus:border-primary"
                        />
                      </div>
                    </label>
                  </div>

                  <section>
                    <div className="mb-3 flex items-center justify-between">
                      <div>
                        <h4 className="text-sm font-semibold">Quote items</h4>
                        <p className="mt-1 text-xs text-muted-foreground">
                          Use a saved catalogue item or leave it as Custom.
                        </p>
                      </div>
                      <button
                        type="button"
                        onClick={() => setLines((current) => [...current, makeLine()])}
                        className="inline-flex h-9 items-center gap-2 rounded-lg border border-border px-3 text-xs font-semibold hover:bg-muted"
                      >
                        <Plus className="size-3.5" />
                        Add line
                      </button>
                    </div>

                    <div className="space-y-3">
                      {lines.map((line, index) => (
                        <div
                          key={line.key}
                          className="rounded-2xl border border-border bg-background p-4"
                        >
                          <div className="mb-3 flex items-center justify-between">
                            <p className="text-xs font-semibold text-muted-foreground">
                              LINE {index + 1}
                            </p>
                            <button
                              type="button"
                              onClick={() => removeLine(line.key)}
                              disabled={lines.length === 1}
                              className="inline-flex size-8 items-center justify-center rounded-lg text-muted-foreground hover:bg-[var(--status-danger-bg)] hover:text-[var(--status-danger)] disabled:opacity-30"
                            >
                              <Trash2 className="size-3.5" />
                            </button>
                          </div>

                          <div className="grid gap-3 lg:grid-cols-[1.3fr_1.5fr_0.55fr_0.75fr_0.75fr]">
                            <label className="block">
                              <span className="mb-1.5 block text-xs text-muted-foreground">
                                Catalogue item
                              </span>
                              <select
                                value={line.catalogueItemId}
                                onChange={(event) =>
                                  selectCatalogueItem(line.key, event.target.value)
                                }
                                className="h-10 w-full rounded-lg border border-input bg-card px-2.5 text-xs outline-none focus:border-primary"
                              >
                                <option value="">Custom item</option>
                                {catalogue.map((item) => (
                                  <option key={item.id} value={item.id}>
                                    {item.name}
                                  </option>
                                ))}
                              </select>
                            </label>

                            <label className="block">
                              <span className="mb-1.5 block text-xs text-muted-foreground">
                                Description
                              </span>
                              <input
                                value={line.description}
                                onChange={(event) =>
                                  updateLine(line.key, "description", event.target.value)
                                }
                                placeholder="Work or item description"
                                className="h-10 w-full rounded-lg border border-input bg-card px-3 text-xs outline-none focus:border-primary"
                              />
                            </label>

                            <label className="block">
                              <span className="mb-1.5 block text-xs text-muted-foreground">
                                Qty
                              </span>
                              <input
                                type="number"
                                min="0.01"
                                step="0.01"
                                value={line.quantity}
                                onChange={(event) =>
                                  updateLine(line.key, "quantity", event.target.value)
                                }
                                className="h-10 w-full rounded-lg border border-input bg-card px-3 text-xs outline-none focus:border-primary"
                              />
                            </label>

                            <label className="block">
                              <span className="mb-1.5 block text-xs text-muted-foreground">
                                Unit price
                              </span>
                              <input
                                type="number"
                                min="0"
                                step="0.01"
                                value={line.unitPrice}
                                onChange={(event) =>
                                  updateLine(line.key, "unitPrice", event.target.value)
                                }
                                placeholder="0.00"
                                className="h-10 w-full rounded-lg border border-input bg-card px-3 text-xs outline-none focus:border-primary"
                              />
                            </label>

                            <label className="block">
                              <span className="mb-1.5 block text-xs text-muted-foreground">
                                Discount
                              </span>
                              <input
                                type="number"
                                min="0"
                                step="0.01"
                                value={line.discountAmount}
                                onChange={(event) =>
                                  updateLine(line.key, "discountAmount", event.target.value)
                                }
                                className="h-10 w-full rounded-lg border border-input bg-card px-3 text-xs outline-none focus:border-primary"
                              />
                            </label>
                          </div>
                        </div>
                      ))}
                    </div>

                    <div className="mt-3 flex justify-end">
                      <div className="rounded-xl bg-muted px-4 py-3 text-right">
                        <p className="text-[11px] uppercase tracking-wide text-muted-foreground">
                          Estimated subtotal
                        </p>
                        <p className="mt-1 text-lg font-semibold">
                          {formatMoney(estimatedSubtotal, "ZAR")}
                        </p>
                        <p className="mt-1 text-[10px] text-muted-foreground">
                          Final tax and totals are calculated by the API.
                        </p>
                      </div>
                    </div>
                  </section>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <label className="block">
                      <span className="mb-2 block text-sm font-medium">Tax rate (%)</span>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={taxRate}
                        onChange={(event) => setTaxRate(event.target.value)}
                        placeholder="Use business default"
                        className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                      />
                    </label>
                    <div className="rounded-xl bg-accent p-3 text-xs leading-5 text-accent-foreground">
                      Leave tax blank to use the business VAT/tax configuration already stored in QuoteSnap.
                    </div>
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <label className="block">
                      <span className="mb-2 block text-sm font-medium">Notes</span>
                      <textarea
                        value={notes}
                        onChange={(event) => setNotes(event.target.value)}
                        rows={3}
                        placeholder="Optional note to the customer"
                        className="w-full resize-none rounded-xl border border-input bg-background px-4 py-3 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                      />
                    </label>
                    <label className="block">
                      <span className="mb-2 block text-sm font-medium">Terms</span>
                      <textarea
                        value={terms}
                        onChange={(event) => setTerms(event.target.value)}
                        rows={3}
                        placeholder="e.g. 50% upfront, 50% on completion"
                        className="w-full resize-none rounded-xl border border-input bg-background px-4 py-3 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                      />
                    </label>
                  </div>

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
                      onClick={closeForm}
                      disabled={isSaving}
                      className="h-10 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-muted disabled:opacity-50"
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={isSaving || customers.length === 0}
                      className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-60"
                    >
                      {isSaving && <LoaderCircle className="size-4 animate-spin" />}
                      Create quote
                    </button>
                  </div>
                </>
              )}
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
