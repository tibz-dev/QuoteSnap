export type LoginRequest = {
  email: string
  password: string
}

export type RegisterRequest = {
  firstName: string
  lastName: string
  email: string
  password: string
  businessName: string
  countryCode: string
  currencyCode: string
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

export type ForgotPasswordRequest = {
  email: string
}

export type ResetPasswordRequest = {
  userId: string
  token: string
  newPassword: string
}

export type MessageResponse = {
  message: string
}
