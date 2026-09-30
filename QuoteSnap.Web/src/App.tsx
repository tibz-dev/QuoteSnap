import { useEffect, useState } from "react"
import { AuthPage } from "@/features/auth/AuthPage"
import {
  clearAuthSession,
  getAuthSession,
  saveAuthSession,
} from "@/features/auth/auth.storage"
import type { AuthSession } from "@/features/auth/auth.types"
import { DashboardShell } from "@/features/dashboard/DashboardShell"

function App() {
  const [isDark, setIsDark] = useState(false)
  const [session, setSession] = useState<AuthSession | null>(() =>
    getAuthSession(),
  )

  useEffect(() => {
    document.documentElement.classList.toggle("dark", isDark)
  }, [isDark])

  const handleAuthenticated = (authSession: AuthSession) => {
    saveAuthSession(authSession)
    setSession(authSession)
  }

  const handleSignOut = () => {
    clearAuthSession()
    setSession(null)
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
