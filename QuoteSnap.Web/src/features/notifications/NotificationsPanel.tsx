import {
  AlertCircle,
  CheckCircle2,
  Clock3,
  LoaderCircle,
  Mail,
  RefreshCw,
  X,
} from "lucide-react"
import type { Notification } from "./notification.types"

const statusLabels: Record<number, string> = {
  1: "Pending",
  2: "Processing",
  3: "Sent",
  4: "Failed",
  5: "Cancelled",
}

export function NotificationsPanel({
  open,
  notifications,
  isLoading,
  error,
  onClose,
  onRefresh,
}: {
  open: boolean
  notifications: Notification[]
  isLoading: boolean
  error: string
  onClose: () => void
  onRefresh: () => void
}) {
  if (!open) return null

  return (
    <>
      <button
        type="button"
        onClick={onClose}
        aria-label="Close notifications"
        className="fixed inset-0 z-40 bg-black/20"
      />

      <aside className="fixed right-0 top-0 z-50 flex h-full w-full max-w-md flex-col border-l border-border bg-card shadow-2xl">
        <div className="flex h-20 items-center justify-between border-b border-border px-5">
          <div>
            <h2 className="font-semibold">Notifications</h2>
            <p className="mt-1 text-xs text-muted-foreground">
              Recent QuoteSnap account and delivery activity.
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

        <div className="flex items-center justify-between border-b border-border px-5 py-3">
          <p className="text-xs text-muted-foreground">
            {notifications.length} recent
          </p>
          <button
            type="button"
            onClick={onRefresh}
            disabled={isLoading}
            className="inline-flex items-center gap-2 text-xs font-semibold text-primary disabled:opacity-50"
          >
            <RefreshCw className={`size-3.5 ${isLoading ? "animate-spin" : ""}`} />
            Refresh
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4">
          {error && (
            <div
              className="mb-4 rounded-xl px-4 py-3 text-sm"
              style={{
                color: "var(--status-danger)",
                backgroundColor: "var(--status-danger-bg)",
              }}
            >
              {error}
            </div>
          )}

          {isLoading && notifications.length === 0 ? (
            <div className="flex min-h-64 items-center justify-center">
              <LoaderCircle className="size-6 animate-spin text-primary" />
            </div>
          ) : notifications.length === 0 ? (
            <div className="flex min-h-64 flex-col items-center justify-center text-center">
              <Mail className="size-7 text-muted-foreground" />
              <p className="mt-3 text-sm font-semibold">No notifications yet</p>
              <p className="mt-1 max-w-xs text-xs leading-5 text-muted-foreground">
                Trial, subscription, account and payment notifications will appear here.
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {notifications.map((notification) => {
                const isFailed = notification.status === 4
                const isSent = notification.status === 3
                const StatusIcon = isFailed
                  ? AlertCircle
                  : isSent
                    ? CheckCircle2
                    : Clock3

                return (
                  <article
                    key={notification.id}
                    className="rounded-2xl border border-border bg-background p-4"
                  >
                    <div className="flex items-start gap-3">
                      <span
                        className="flex size-9 shrink-0 items-center justify-center rounded-xl"
                        style={{
                          color: isFailed
                            ? "var(--status-danger)"
                            : isSent
                              ? "var(--status-success)"
                              : "var(--status-warning)",
                          backgroundColor: isFailed
                            ? "var(--status-danger-bg)"
                            : isSent
                              ? "var(--status-success-bg)"
                              : "var(--status-warning-bg)",
                        }}
                      >
                        <StatusIcon className="size-4" />
                      </span>

                      <div className="min-w-0 flex-1">
                        <div className="flex items-start justify-between gap-3">
                          <p className="text-sm font-semibold">
                            {notification.subject}
                          </p>
                          <span className="shrink-0 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
                            {statusLabels[notification.status] || "Activity"}
                          </span>
                        </div>
                        <p className="mt-1 truncate text-xs text-muted-foreground">
                          {notification.recipient}
                        </p>
                        <p className="mt-2 text-[11px] text-muted-foreground">
                          {formatDateTime(notification.createdAt)}
                        </p>
                        {notification.errorMessage && (
                          <p className="mt-2 text-xs leading-5 text-[var(--status-danger)]">
                            {notification.errorMessage}
                          </p>
                        )}
                      </div>
                    </div>
                  </article>
                )
              })}
            </div>
          )}
        </div>
      </aside>
    </>
  )
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("en-ZA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value))
}
