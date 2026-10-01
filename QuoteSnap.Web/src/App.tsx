import { useEffect, useState } from "react"
import { AuthPage } from "@/features/auth/AuthPage"
import {
  clearAuthSession,
  getAuthSession,
  saveAuthSession,
} from "@/features/auth/auth.storage"
import type { AuthSession } from "@/features/auth/auth.types"
import { DashboardShell } from "@/features/dashboard/DashboardShell"

const THEME_STORAGE_KEY = "quotesnap.theme"

function getInitialTheme() {
  const storedTheme = localStorage.getItem(THEME_STORAGE_KEY)

  if (storedTheme === "dark") return true
  if (storedTheme === "light") return false

  return window.matchMedia("(prefers-color-scheme: dark)").matches
}

function App() {
  const [isDark, setIsDark] = useState(getInitialTheme)
  const [session, setSession] = useState<AuthSession | null>(() =>
    getAuthSession(),
  )

  useEffect(() => {
    document.documentElement.classList.toggle("dark", isDark)
    localStorage.setItem(THEME_STORAGE_KEY, isDark ? "dark" : "light")
  }, [isDark])

  useEffect(() => {
    const handleUnauthorized = () => {
      clearAuthSession()
      setSession(null)
    }

    window.addEventListener("quotesnap:unauthorized", handleUnauthorized)

    return () => {
      window.removeEventListener("quotesnap:unauthorized", handleUnauthorized)
    }
  }, [])

  const handleAuthenticated = (authSession: AuthSession) => {
    saveAuthSession(authSession)
    setSession(authSession)
  }

  const handleSignOut = () => {
    clearAuthSession()
    setSession(null)
    window.history.replaceState({}, "", window.location.pathname)
  }

  const toggleTheme = () => {
    setIsDark((current) => !current)
  }

  if (!session) {
    return (
      <AuthPage
        isDark={isDark}
        onToggleTheme={toggleTheme}
        onAuthenticated={handleAuthenticated}
      />
    )
  }

  return (
    <DashboardShell
      session={session}
      isDark={isDark}
      onToggleTheme={toggleTheme}
      onSignOut={handleSignOut}
    />
  )
}

export default App
