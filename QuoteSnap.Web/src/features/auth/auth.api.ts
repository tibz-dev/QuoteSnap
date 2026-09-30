import { api } from "@/lib/api"
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
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
}
