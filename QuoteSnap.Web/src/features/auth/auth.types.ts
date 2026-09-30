export type LoginRequest = {
  email: string
  password: string
}

export type AuthResponse = {
  token: string
  expiresAt: string
  userId: string
  businessId: string
  email: string
  firstName: string
  lastName: string
  role: string
}

export type AuthSession = AuthResponse
