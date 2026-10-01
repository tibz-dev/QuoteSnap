import { useEffect, useState } from "react"
import {
  BadgeDollarSign,
  Bell,
  ChevronDown,
  FileCheck2,
  FileText,
  LayoutDashboard,
  Menu,
  Moon,
  PackageOpen,
  Plus,
  ReceiptText,
  Settings,
  Sun,
  Users,
  WalletCards,
  X,
} from "lucide-react"
import type { AuthSession } from "@/features/auth/auth.types"
import { ServicesPage } from "@/features/catalogue/ServicesPage"
import { CustomersPage } from "@/features/customers/CustomersPage"
import { InvoicesPage } from "@/features/invoices/InvoicesPage"
import { notificationApi } from "@/features/notifications/notification.api"
import { NotificationsPanel } from "@/features/notifications/NotificationsPanel"
import type { Notification } from "@/features/notifications/notification.types"
import { PaymentsPage } from "@/features/payments/PaymentsPage"
import { QuotesPage } from "@/features/quotes/QuotesPage"
import { ReceiptsPage } from "@/features/receipts/ReceiptsPage"
import { SettingsPage } from "@/features/settings/SettingsPage"
import { subscriptionApi } from "@/features/subscription/subscription.api"
import { SubscriptionPage } from "@/features/subscription/SubscriptionPage"
import type { SubscriptionOverview } from "@/features/subscription/subscription.types"
import { DashboardOverview } from "./DashboardOverview"

type DashboardShellProps = {
  session: AuthSession
  isDark: boolean
  onToggleTheme: () => void
  onSignOut: () => void
}

const navigation = [
  { label: "Dashboard", icon: LayoutDashboard },
  { label: "Customers", icon: Users },
  { label: "Services", icon: PackageOpen },
  { label: "Quotes", icon: FileText },
  { label: "Invoices", icon: FileCheck2 },
  { label: "Payments", icon: WalletCards },
  { label: "Receipts", icon: ReceiptText },
  { label: "Subscription", icon: BadgeDollarSign },
  { label: "Settings", icon: Settings },
]

const workspaceViews = new Map(
  navigation.map((item) => [item.label.toLowerCase(), item.label]),
)

function getWorkspaceViewFromUrl() {
  const params = new URLSearchParams(window.location.search)

  if (params.has("settings")) {
    return "Settings"
  }

  if (params.get("subscription") === "return") {
    return "Subscription"
  }

  return workspaceViews.get(params.get("view")?.toLowerCase() || "") ||
    "Dashboard"
}

function updateWorkspaceUrl(label: string, mode: "push" | "replace" = "push") {
  const url = new URL(window.location.href)

  url.search = ""

  if (label !== "Dashboard") {
    url.searchParams.set("view", label.toLowerCase())
  }

  const nextUrl = `${url.pathname}${url.search}${url.hash}`

  if (mode === "replace") {
    window.history.replaceState({}, "", nextUrl)
  } else {
    window.history.pushState({}, "", nextUrl)
  }
}

export function DashboardShell({
  session,
  isDark,
  onToggleTheme,
  onSignOut,
}: DashboardShellProps) {
  const [activeItem, setActiveItem] = useState(getWorkspaceViewFromUrl)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [invoiceCreateSignal, setInvoiceCreateSignal] = useState(0)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [notificationsLoading, setNotificationsLoading] = useState(false)
  const [notificationsError, setNotificationsError] = useState("")
  const [subscriptionOverview, setSubscriptionOverview] =
    useState<SubscriptionOverview | null>(null)

  useEffect(() => {
    void loadNotifications()
    void loadSubscriptionOverview()
  }, [])

  useEffect(() => {
    const handlePopState = () => {
      setActiveItem(getWorkspaceViewFromUrl())
      setMobileMenuOpen(false)
    }

    window.addEventListener("popstate", handlePopState)

    return () => {
      window.removeEventListener("popstate", handlePopState)
    }
  }, [])

  useEffect(() => {
    document.title = `${activeItem} | QuoteSnap`

    if (activeItem === "Subscription") {
      void loadSubscriptionOverview()
    }
  }, [activeItem])

  async function loadNotifications() {
    try {
      setNotificationsLoading(true)
      setNotificationsError("")
      setNotifications(await notificationApi.getRecent())
    } catch (error) {
      setNotificationsError(
        error instanceof Error
          ? error.message
          : "We couldn't load notifications.",
      )
    } finally {
      setNotificationsLoading(false)
    }
  }

  async function loadSubscriptionOverview() {
    try {
      setSubscriptionOverview(await subscriptionApi.getOverview())
    } catch {
      setSubscriptionOverview(null)
    }
  }

  const attentionCount = notifications.filter((notification) => {
    if (notification.status === 4) {
      return true
    }

    if (notification.status !== 1 && notification.status !== 2) {
      return false
    }

    return new Date(notification.scheduledFor).getTime() <= Date.now()
  }).length

  const handleNewInvoice = () => {
    if (activeItem !== "Invoices") {
      updateWorkspaceUrl("Invoices")
    }

    setActiveItem("Invoices")
    setMobileMenuOpen(false)
    setInvoiceCreateSignal((current) => current + 1)
  }

  const displayName =
    [session.firstName, session.lastName].filter(Boolean).join(" ") ||
    session.email

  const initials =
    [session.firstName, session.lastName]
      .filter(Boolean)
      .map((name) => name[0])
      .join("")
      .slice(0, 2)
      .toUpperCase() || "QS"

  const selectItem = (label: string) => {
    if (label !== activeItem) {
      updateWorkspaceUrl(label)
    }

    setActiveItem(label)
    setMobileMenuOpen(false)
  }

  return (
    <main className="min-h-screen bg-background text-foreground">
      {mobileMenuOpen && (
        <button
          type="button"
          aria-label="Close navigation"
          onClick={() => setMobileMenuOpen(false)}
          className="fixed inset-0 z-30 bg-black/30 lg:hidden"
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-[270px] flex-col border-r border-sidebar-border bg-sidebar transition-transform duration-200 lg:translate-x-0 ${
          mobileMenuOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="flex h-20 items-center justify-between px-5">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-xl bg-sidebar-primary text-sidebar-primary-foreground shadow-sm">
              <FileText className="size-5" strokeWidth={2.2} />
            </span>
            <div>
              <p className="text-lg font-semibold tracking-[-0.03em]">
                QuoteSnap
              </p>
              <p className="text-[11px] font-medium uppercase tracking-[0.12em] text-muted-foreground">
                Workspace
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={() => setMobileMenuOpen(false)}
            className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-sidebar-accent lg:hidden"
          >
            <X className="size-4.5" />
          </button>
        </div>

        <nav className="flex-1 space-y-1 px-3 py-3">
          {navigation.map(({ label, icon: Icon }) => {
            const isActive = activeItem === label

            return (
              <button
                key={label}
                type="button"
                onClick={() => selectItem(label)}
                className={`flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-sm font-medium transition ${
                  isActive
                    ? "bg-sidebar-accent text-sidebar-accent-foreground"
                    : "text-muted-foreground hover:bg-sidebar-accent/70 hover:text-foreground"
                }`}
              >
                <Icon className="size-[18px]" strokeWidth={1.9} />
                {label}
              </button>
            )
          })}
        </nav>

        <div className="border-t border-sidebar-border p-3">
          <div className="rounded-2xl bg-card p-3">
            <div className="flex items-center gap-3">
              <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary text-xs font-semibold text-primary-foreground">
                {initials}
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-semibold">{displayName}</p>
                <p className="truncate text-xs text-muted-foreground">
                  {session.email}
                </p>
              </div>
              <button
                type="button"
                onClick={() => selectItem("Settings")}
                className="inline-flex size-8 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted hover:text-foreground"
                aria-label="Open settings"
              >
                <ChevronDown className="size-4" />
              </button>
            </div>
            <button
              type="button"
              onClick={onSignOut}
              className="mt-3 w-full rounded-lg border border-border px-3 py-2 text-xs font-semibold text-muted-foreground transition hover:bg-muted hover:text-foreground"
            >
              Sign out
            </button>
          </div>
        </div>
      </aside>

      <div className="lg:pl-[270px]">
        <header className="sticky top-0 z-20 flex h-20 items-center justify-between border-b border-border bg-background/90 px-4 backdrop-blur sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setMobileMenuOpen(true)}
              className="inline-flex size-10 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground lg:hidden"
              aria-label="Open navigation"
            >
              <Menu className="size-5" />
            </button>
            <div>
              <p className="text-sm text-muted-foreground">Workspace</p>
              <h1 className="text-lg font-semibold tracking-[-0.025em]">
                {activeItem}
              </h1>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => {
                setNotificationsOpen(true)
                void loadNotifications()
              }}
              className="relative inline-flex size-10 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground transition hover:bg-accent hover:text-accent-foreground"
              aria-label="Open notifications"
            >
              <Bell className="size-4.5" />
              {attentionCount > 0 && (
                <span className="absolute right-1.5 top-1.5 flex min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[9px] font-semibold leading-4 text-primary-foreground">
                  {attentionCount > 9 ? "9+" : attentionCount}
                </span>
              )}
            </button>

            <button
              type="button"
              onClick={onToggleTheme}
              className="inline-flex size-10 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground transition hover:bg-accent hover:text-accent-foreground"
              aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
            >
              {isDark ? (
                <Sun className="size-4.5" />
              ) : (
                <Moon className="size-4.5" />
              )}
            </button>

            <button
              type="button"
              onClick={handleNewInvoice}
              className="ml-1 hidden h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground shadow-sm transition hover:opacity-95 sm:inline-flex"
            >
              <Plus className="size-4" />
              New invoice
            </button>
          </div>
        </header>

        {subscriptionOverview?.isReadOnly && (
          <div
            className="flex flex-col gap-3 border-b px-4 py-3 text-sm sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8"
            style={{
              color: "var(--status-danger)",
              backgroundColor: "var(--status-danger-bg)",
              borderColor: "var(--border)",
            }}
          >
            <span>
              {subscriptionOverview.accessMessage ||
                "Your subscription is read-only until billing is resolved."}
            </span>
            <button
              type="button"
              onClick={() => selectItem("Subscription")}
              className="self-start font-semibold underline sm:self-auto"
            >
              Manage subscription
            </button>
          </div>
        )}

        {!subscriptionOverview?.isReadOnly &&
          subscriptionOverview?.subscription.status === 1 &&
          (subscriptionOverview.subscription.trialDaysRemaining ?? 99) <= 3 && (
            <div
              className="flex flex-col gap-3 border-b px-4 py-3 text-sm sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8"
              style={{
                color: "var(--status-warning)",
                backgroundColor: "var(--status-warning-bg)",
                borderColor: "var(--border)",
              }}
            >
              <span>
                Your QuoteSnap trial ends in{" "}
                {subscriptionOverview.subscription.trialDaysRemaining ?? 0}{" "}
                days.
              </span>
              <button
                type="button"
                onClick={() => selectItem("Subscription")}
                className="self-start font-semibold underline sm:self-auto"
              >
                View plans
              </button>
            </div>
          )}

        {activeItem === "Dashboard" && (
          <DashboardOverview
            displayName={session.firstName || displayName}
            onNavigate={selectItem}
            onCreateInvoice={handleNewInvoice}
          />
        )}

        {activeItem === "Customers" && <CustomersPage />}

        {activeItem === "Services" && <ServicesPage />}

        {activeItem === "Quotes" && (
          <QuotesPage onNavigate={selectItem} />
        )}

        {activeItem === "Invoices" && (
          <InvoicesPage
            createSignal={invoiceCreateSignal}
            onNavigate={selectItem}
          />
        )}

        {activeItem === "Payments" && (
          <PaymentsPage onNavigate={selectItem} />
        )}

        {activeItem === "Receipts" && <ReceiptsPage />}

        {activeItem === "Subscription" && <SubscriptionPage />}

        {activeItem === "Settings" && (
          <SettingsPage onNavigate={selectItem} />
        )}


      </div>

      <NotificationsPanel
        open={notificationsOpen}
        notifications={notifications}
        isLoading={notificationsLoading}
        error={notificationsError}
        onClose={() => setNotificationsOpen(false)}
        onRefresh={() => void loadNotifications()}
      />
    </main>
  )
}
