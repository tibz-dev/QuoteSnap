import { api } from "@/lib/api"
import type { AuthResponse, LoginRequest } from "./auth.types"

export const authApi = {
  login(request: LoginRequest) {
    return api.post<AuthResponse, LoginRequest>("/api/auth/login", request)
  },
}
