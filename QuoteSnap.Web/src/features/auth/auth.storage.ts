import type { AuthSession } from "./auth.types"

const AUTH_STORAGE_KEY = "quotesnap.auth"

export function saveAuthSession(session: AuthSession) {
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
}

export function getAuthSession(): AuthSession | null {
  const storedSession = localStorage.getItem(AUTH_STORAGE_KEY)

  if (!storedSession) {
    return null
  }

  try {
    const session = JSON.parse(storedSession) as AuthSession
    const expiresAt = new Date(session.expiresAt).getTime()

    if (!session.token || Number.isNaN(expiresAt) || expiresAt <= Date.now()) {
      clearAuthSession()
      return null
    }

    return session
  } catch {
    clearAuthSession()
    return null
  }
}

export function clearAuthSession() {
  localStorage.removeItem(AUTH_STORAGE_KEY)
}
