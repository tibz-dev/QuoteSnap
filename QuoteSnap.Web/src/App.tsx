import { useEffect, useState, type FormEvent } from "react"
import {
  ArrowRight,
  Check,
  FileText,
  LockKeyhole,
  Mail,
  Moon,
  Sun,
} from "lucide-react"

type AuthMode = "login" | "register"

function App() {
  const [mode, setMode] = useState<AuthMode>("login")
  const [isDark, setIsDark] = useState(false)

  useEffect(() => {
    document.documentElement.classList.toggle("dark", isDark)
  }, [isDark])

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
  }

  return (
    <main className="min-h-screen bg-background text-foreground lg:grid lg:grid-cols-[minmax(0,0.92fr)_minmax(520px,1.08fr)]">
      <section className="relative flex min-h-screen flex-col px-6 py-6 sm:px-10 lg:px-14 xl:px-20">
        <header className="flex items-center justify-between">
          <a href="/" className="flex items-center gap-3" aria-label="QuoteSnap home">
            <span className="flex size-10 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm">
              <FileText className="size-5" strokeWidth={2.2} />
            </span>
            <span className="text-xl font-semibold tracking-[-0.03em]">QuoteSnap</span>
          </a>

          <button
            type="button"
            onClick={() => setIsDark((current) => !current)}
            className="inline-flex size-10 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground transition hover:bg-accent hover:text-accent-foreground"
            aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
          >
            {isDark ? <Sun className="size-4.5" /> : <Moon className="size-4.5" />}
          </button>
        </header>

        <div className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center py-14">
          <div className="mb-8">
            <span className="mb-4 inline-flex rounded-full bg-accent px-3 py-1 text-xs font-semibold tracking-wide text-accent-foreground">
              SIMPLE BUSINESS DOCUMENTS
            </span>
            <h1 className="text-4xl font-semibold tracking-[-0.045em] sm:text-[2.7rem]">
              {mode === "login" ? "Welcome back." : "Start sending better invoices."}
            </h1>
            <p className="mt-3 max-w-sm text-[15px] leading-6 text-muted-foreground">
              {mode === "login"
                ? "Sign in to manage your quotes, invoices, customers and payments."
                : "Create your QuoteSnap workspace and get your business documents organised."}
            </p>
          </div>

          <div className="mb-7 grid grid-cols-2 rounded-xl bg-muted p-1">
            <button
              type="button"
              onClick={() => setMode("login")}
              className={`rounded-lg px-4 py-2.5 text-sm font-medium transition ${
                mode === "login"
                  ? "bg-card text-foreground shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              }`}
            >
              Sign in
            </button>
            <button
              type="button"
              onClick={() => setMode("register")}
              className={`rounded-lg px-4 py-2.5 text-sm font-medium transition ${
                mode === "register"
                  ? "bg-card text-foreground shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              }`}
            >
              Create account
            </button>
          </div>

          <form className="space-y-4" onSubmit={handleSubmit}>
            {mode === "register" && (
              <>
                <div>
                  <label htmlFor="fullName" className="mb-2 block text-sm font-medium">
                    Full name
                  </label>
                  <input
                    id="fullName"
                    name="fullName"
                    autoComplete="name"
                    placeholder="Tebatso Seshayi"
                    className="h-12 w-full rounded-xl border border-input bg-card px-4 text-sm outline-none transition placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
                  />
                </div>

                <div>
                  <label htmlFor="businessName" className="mb-2 block text-sm font-medium">
                    Business name
                  </label>
                  <input
                    id="businessName"
                    name="businessName"
                    autoComplete="organization"
                    placeholder="Your business name"
                    className="h-12 w-full rounded-xl border border-input bg-card px-4 text-sm outline-none transition placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
                  />
                </div>
              </>
            )}

            <div>
              <label htmlFor="email" className="mb-2 block text-sm font-medium">
                Email address
              </label>
              <div className="relative">
                <Mail className="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  id="email"
                  name="email"
                  type="email"
                  autoComplete="email"
                  placeholder="you@business.co.za"
                  className="h-12 w-full rounded-xl border border-input bg-card pl-11 pr-4 text-sm outline-none transition placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
                />
              </div>
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <label htmlFor="password" className="text-sm font-medium">
                  Password
                </label>
                {mode === "login" && (
                  <button type="button" className="text-sm font-medium text-primary hover:underline">
                    Forgot password?
                  </button>
                )}
              </div>
              <div className="relative">
                <LockKeyhole className="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  id="password"
                  name="password"
                  type="password"
                  autoComplete={mode === "login" ? "current-password" : "new-password"}
                  placeholder={mode === "login" ? "Enter your password" : "Create a secure password"}
                  className="h-12 w-full rounded-xl border border-input bg-card pl-11 pr-4 text-sm outline-none transition placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
                />
              </div>
            </div>

            <button
              type="submit"
              className="group mt-2 inline-flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-primary px-5 text-sm font-semibold text-primary-foreground shadow-sm transition hover:opacity-95 focus:outline-none focus:ring-3 focus:ring-primary/20"
            >
              {mode === "login" ? "Sign in to QuoteSnap" : "Create my workspace"}
              <ArrowRight className="size-4 transition-transform group-hover:translate-x-0.5" />
            </button>
          </form>

          <p className="mt-6 text-center text-xs leading-5 text-muted-foreground">
            {mode === "register"
              ? "By creating an account, you agree to our Terms and Privacy Policy."
              : "Secure access to your QuoteSnap workspace."}
          </p>
        </div>

        <footer className="text-xs text-muted-foreground">
          © {new Date().getFullYear()} QuoteSnap. Built for small businesses.
        </footer>
      </section>

      <aside className="relative hidden overflow-hidden border-l border-border bg-sidebar lg:flex lg:min-h-screen lg:flex-col lg:justify-between">
        <div className="pointer-events-none absolute -right-32 -top-32 size-[420px] rounded-full bg-primary/10 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-40 left-0 size-[360px] rounded-full bg-accent blur-3xl" />

        <div className="relative z-10 px-12 pt-16 xl:px-16 xl:pt-20">
          <p className="text-sm font-semibold text-primary">FROM QUOTE TO PAYMENT</p>
          <h2 className="mt-4 max-w-xl text-4xl font-semibold leading-[1.08] tracking-[-0.045em] xl:text-5xl">
            Run the paperwork side of your business without the paperwork.
          </h2>
          <p className="mt-5 max-w-lg text-base leading-7 text-muted-foreground">
            Create professional documents, keep track of what clients owe you and stay on top of your business from one clean workspace.
          </p>

          <div className="mt-8 flex flex-wrap gap-x-6 gap-y-3 text-sm text-muted-foreground">
            {["Professional PDFs", "Payment tracking", "Customer records"].map((item) => (
              <span key={item} className="flex items-center gap-2">
                <span className="flex size-5 items-center justify-center rounded-full bg-accent text-accent-foreground">
                  <Check className="size-3" strokeWidth={3} />
                </span>
                {item}
              </span>
            ))}
          </div>
        </div>

        <div className="relative z-10 mx-12 mb-14 mt-12 rounded-3xl border border-border bg-card p-5 shadow-[0_30px_80px_rgba(20,34,29,0.10)] xl:mx-16 xl:mb-16">
          <div className="flex items-start justify-between border-b border-border pb-5">
            <div>
              <p className="text-xs font-medium uppercase tracking-[0.12em] text-muted-foreground">Invoice</p>
              <p className="mt-1 text-lg font-semibold">INV-000128</p>
            </div>
            <span
              className="rounded-full px-3 py-1 text-xs font-semibold"
              style={{
                color: "var(--status-success)",
                backgroundColor: "var(--status-success-bg)",
              }}
            >
              Paid
            </span>
          </div>

          <div className="grid grid-cols-2 gap-5 py-5">
            <div>
              <p className="text-xs text-muted-foreground">Billed to</p>
              <p className="mt-1 text-sm font-semibold">Mokoena Trading</p>
            </div>
            <div className="text-right">
              <p className="text-xs text-muted-foreground">Invoice total</p>
              <p className="mt-1 text-xl font-semibold">R 5,250.00</p>
            </div>
          </div>

          <div className="rounded-2xl bg-muted p-4">
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Website development</span>
              <span className="font-medium">R 5,250.00</span>
            </div>
            <div className="mt-4 flex items-center justify-between border-t border-border pt-4">
              <span className="text-sm font-semibold">Balance due</span>
              <span className="text-lg font-semibold text-primary">R 0.00</span>
            </div>
          </div>
        </div>
      </aside>
    </main>
  )
}

export default App
