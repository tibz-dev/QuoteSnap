import { api } from "@/lib/api"
import type {
  BusinessSettings,
  EmailConnection,
  InitializeSubscriptionResponse,
  Subscription,
} from "./settings.types"

export const settingsApi = {
  getBusiness() {
    return api.get<BusinessSettings>("/api/business")
  },

  updateBusiness(request: BusinessSettings) {
    return api.put<BusinessSettings, BusinessSettings>("/api/business", request)
  },

  getEmailConnections() {
    return api.get<EmailConnection[]>("/api/email-connections")
  },

  getGoogleConnectUrl() {
    return api.get<{ authorizationUrl: string }>(
      "/api/email-connections/google/connect",
    )
  },

  disconnectGoogle() {
    return api.delete("/api/email-connections/google")
  },

  getSubscription() {
    return api.get<Subscription>("/api/subscription")
  },

  initializeSubscription(plan: number) {
    return api.post<InitializeSubscriptionResponse, { plan: number }>(
      "/api/subscription-payments/initialize",
      { plan },
    )
  },
}
