import { useEffect, useMemo, useState } from "react"
import {
  Download,
  FileText,
  LoaderCircle,
  Search,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { receiptApi } from "./receipt.api"
import type { Receipt } from "./receipt.types"

const paymentMethodLabels: Record<number, string> = {
  1: "Cash",
  2: "Bank transfer",
  3: "Card",
  4: "Mobile money",
  5: "Other",
}

export function ReceiptsPage() {
  const [receipts, setReceipts] = useState<Receipt[]>([])
  const [search, setSearch] = useState("")
  const [isLoading, setIsLoading] = useState(true)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const [error, setError] = useState("")

  useEffect(() => {
    void loadReceipts()
  }, [])

  async function loadReceipts() {
    try {
      setIsLoading(true)
      setError("")
      setReceipts(await receiptApi.getAll())
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your receipts.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const filtered = useMemo(() => {
    const query = search.trim().toLowerCase()
    if (!query) return receipts

    return receipts.filter((receipt) =>
      [
        receipt.receiptNumber,
        receipt.invoiceNumber,
        receipt.customerName,
        receipt.paymentReference,
      ]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query)),
    )
  }, [receipts, search])

  const handleDownload = async (receipt: Receipt) => {
    try {
      setDownloadingId(receipt.id)
      setError("")
      const { blob, fileName } = await receiptApi.downloadPdf(receipt.id)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement("a")
      anchor.href = url
      anchor.download = fileName
      document.body.appendChild(anchor)
      anchor.click()
      anchor.remove()
      URL.revokeObjectURL(url)
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't download this receipt.",
      )
    } finally {
      setDownloadingId(null)
    }
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div>
          <p className="text-sm font-semibold text-primary">RECEIPTS</p>
          <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
            Payment receipts
          </h2>
          <p className="mt-2 text-sm text-muted-foreground">
            Receipts generated from recorded invoice payments.
          </p>
        </div>

        <div className="mt-8 rounded-2xl border border-border bg-card">
          <div className="flex flex-col gap-4 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative w-full sm:max-w-sm">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search receipt, invoice or customer..."
                className="h-10 w-full rounded-xl border border-input bg-background pl-10 pr-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
              />
            </div>
            <p className="text-xs text-muted-foreground">
              {receipts.length} {receipts.length === 1 ? "receipt" : "receipts"}
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
          ) : filtered.length === 0 ? (
            <div className="flex min-h-72 flex-col items-center justify-center px-6 py-12 text-center">
              <span className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
                <FileText className="size-5" />
              </span>
              <p className="mt-4 text-sm font-semibold">
                {search ? "No matching receipts" : "No receipts yet"}
              </p>
              <p className="mt-1 max-w-sm text-xs leading-5 text-muted-foreground">
                Record a payment, then generate its receipt from the Payments section.
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[900px] text-left">
                <thead>
                  <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                    <th className="px-5 py-3 font-medium">Receipt</th>
                    <th className="px-5 py-3 font-medium">Invoice</th>
                    <th className="px-5 py-3 font-medium">Customer</th>
                    <th className="px-5 py-3 font-medium">Method</th>
                    <th className="px-5 py-3 font-medium">Payment date</th>
                    <th className="px-5 py-3 text-right font-medium">Amount</th>
                    <th className="px-5 py-3 text-right font-medium">PDF</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((receipt) => (
                    <tr
                      key={receipt.id}
                      className="border-b border-border last:border-0 hover:bg-muted/30"
                    >
                      <td className="px-5 py-4">
                        <p className="text-sm font-semibold">{receipt.receiptNumber}</p>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {formatDate(receipt.receiptDate)}
                        </p>
                      </td>
                      <td className="px-5 py-4 text-sm">{receipt.invoiceNumber}</td>
                      <td className="px-5 py-4 text-sm">{receipt.customerName}</td>
                      <td className="px-5 py-4 text-xs text-muted-foreground">
                        {paymentMethodLabels[receipt.paymentMethod] || "Other"}
                      </td>
                      <td className="px-5 py-4 text-xs text-muted-foreground">
                        {formatDate(receipt.paymentDate)}
                      </td>
                      <td className="px-5 py-4 text-right text-sm font-semibold">
                        {formatMoney(receipt.amount, receipt.currencyCode)}
                      </td>
                      <td className="px-5 py-4 text-right">
                        <button
                          type="button"
                          disabled={downloadingId === receipt.id}
                          onClick={() => void handleDownload(receipt)}
                          className="inline-flex h-9 items-center gap-2 rounded-lg border border-border px-3 text-xs font-semibold hover:bg-muted disabled:opacity-50"
                        >
                          {downloadingId === receipt.id ? (
                            <LoaderCircle className="size-3.5 animate-spin" />
                          ) : (
                            <Download className="size-3.5" />
                          )}
                          Download
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
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
