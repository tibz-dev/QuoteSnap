import { useEffect, useState } from "react"
import {
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
  { label: "Settings", icon: Settings },
]

export function DashboardShell({
  session,
  isDark,
  onToggleTheme,
  onSignOut,
}: DashboardShellProps) {
  const [activeItem, setActiveItem] = useState(() => {
    const params = new URLSearchParams(window.location.search)

    return params.has("settings") ||
      params.get("subscription") === "return"
      ? "Settings"
      : "Dashboard"
  })
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [invoiceCreateSignal, setInvoiceCreateSignal] = useState(0)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [notificationsLoading, setNotificationsLoading] = useState(false)
  const [notificationsError, setNotificationsError] = useState("")

  useEffect(() => {
    void loadNotifications()
  }, [])

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

  const attentionCount = notifications.filter(
    (notification) =>
      notification.status === 1 ||
      notification.status === 2 ||
      notification.status === 4,
  ).length

  const handleNewInvoice = () => {
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

        {activeItem === "Settings" && <SettingsPage />}

        {![
          "Dashboard",
          "Customers",
          "Services",
          "Quotes",
          "Invoices",
          "Payments",
          "Receipts",
          "Settings",
        ].includes(activeItem) && <ComingSoonPage title={activeItem} />}
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

function ComingSoonPage({ title }: { title: string }) {
  return (
    <section className="px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-[1500px] rounded-3xl border border-dashed border-border bg-card px-6 py-20 text-center">
        <p className="text-sm font-semibold text-primary">NEXT MODULE</p>
        <h2 className="mt-2 text-2xl font-semibold tracking-[-0.035em]">
          {title}
        </h2>
        <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-muted-foreground">
          This module is next in the frontend rollout.
        </p>
      </div>
    </section>
  )
}
