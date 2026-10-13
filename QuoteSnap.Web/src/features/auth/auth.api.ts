import { api } from "@/lib/api"
import type {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  MessageResponse,
  RegisterRequest,
  ResetPasswordRequest,
} from "./auth.types"

export const authApi = {
  login(request: LoginRequest) {
    return api.post<AuthResponse, LoginRequest>("/api/auth/login", request)
  },

  register(request: RegisterRequest) {
    return api.post<AuthResponse, RegisterRequest>(
      "/api/auth/register",
      request,
    )
  },

  forgotPassword(request: ForgotPasswordRequest) {
    return api.post<MessageResponse, ForgotPasswordRequest>(
      "/api/auth/forgot-password",
      request,
    )
  },

  resetPassword(request: ResetPasswordRequest) {
    return api.post<MessageResponse, ResetPasswordRequest>(
      "/api/auth/reset-password",
      request,
    )
  },
}
