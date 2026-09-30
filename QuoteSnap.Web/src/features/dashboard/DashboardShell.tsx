import { useState } from "react"
import {
  Bell,
  ChevronDown,
  CircleDollarSign,
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
  const [activeItem, setActiveItem] = useState("Dashboard")
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const displayName =
    [session.firstName, session.lastName].filter(Boolean).join(" ") ||
    session.email

  const initials = [session.firstName, session.lastName]
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
              <ChevronDown className="size-4 text-muted-foreground" />
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
              className="relative inline-flex size-10 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground transition hover:bg-accent hover:text-accent-foreground"
              aria-label="Notifications"
            >
              <Bell className="size-4.5" />
              <span className="absolute right-2.5 top-2.5 size-1.5 rounded-full bg-primary" />
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
              className="ml-1 hidden h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground shadow-sm transition hover:opacity-95 sm:inline-flex"
            >
              <Plus className="size-4" />
              New invoice
            </button>
          </div>
        </header>

        {activeItem === "Dashboard" ? (
          <DashboardOverview displayName={session.firstName || displayName} />
        ) : (
          <ComingSoonPage title={activeItem} />
        )}
      </div>
    </main>
  )
}

function DashboardOverview({ displayName }: { displayName: string }) {
  const cards = [
    {
      label: "Revenue",
      value: "R 0.00",
      helper: "Paid this month",
      icon: CircleDollarSign,
    },
    {
      label: "Outstanding",
      value: "R 0.00",
      helper: "Awaiting payment",
      icon: WalletCards,
    },
    {
      label: "Invoices",
      value: "0",
      helper: "Created this month",
      icon: FileCheck2,
    },
    {
      label: "Customers",
      value: "0",
      helper: "Active customers",
      icon: Users,
    },
  ]

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-sm font-medium text-primary">OVERVIEW</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
              Good to see you, {displayName}.
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Here’s what’s happening with your business today.
            </p>
          </div>
          <button
            type="button"
            className="inline-flex h-10 items-center justify-center gap-2 self-start rounded-xl border border-border bg-card px-4 text-sm font-semibold transition hover:bg-muted sm:hidden"
          >
            <Plus className="size-4" />
            New invoice
          </button>
        </div>

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
          <section className="rounded-2xl border border-border bg-card">
            <div className="flex items-center justify-between border-b border-border px-5 py-4">
              <div>
                <h3 className="font-semibold">Recent invoices</h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  Your latest invoice activity will appear here.
                </p>
              </div>
              <button
                type="button"
                className="text-xs font-semibold text-primary hover:underline"
              >
                View all
              </button>
            </div>

            <div className="flex min-h-64 flex-col items-center justify-center px-6 py-10 text-center">
              <span className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
                <FileCheck2 className="size-5" />
              </span>
              <p className="mt-4 text-sm font-semibold">No invoices yet</p>
              <p className="mt-1 max-w-xs text-xs leading-5 text-muted-foreground">
                Create your first invoice and QuoteSnap will track it here.
              </p>
              <button
                type="button"
                className="mt-5 inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
              >
                <Plus className="size-3.5" />
                Create invoice
              </button>
            </div>
          </section>

          <section className="rounded-2xl border border-border bg-card p-5">
            <h3 className="font-semibold">Quick actions</h3>
            <p className="mt-1 text-xs text-muted-foreground">
              Common things you’ll do in QuoteSnap.
            </p>

            <div className="mt-5 space-y-2">
              {[
                ["Create a quote", FileText],
                ["Add a customer", Users],
                ["Record a payment", WalletCards],
                ["Create a receipt", ReceiptText],
              ].map(([label, Icon]) => {
                const ActionIcon = Icon as typeof FileText

                return (
                  <button
                    key={label as string}
                    type="button"
                    className="flex w-full items-center gap-3 rounded-xl border border-border bg-background px-3.5 py-3 text-left text-sm font-medium transition hover:bg-muted"
                  >
                    <span className="flex size-9 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                      <ActionIcon className="size-4" />
                    </span>
                    {label as string}
                  </button>
                )
              })}
            </div>
          </section>
        </div>
      </div>
    </section>
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
          The navigation foundation is ready. We’ll build this module against
          the existing QuoteSnap API in the next development half.
        </p>
      </div>
    </section>
  )
}
