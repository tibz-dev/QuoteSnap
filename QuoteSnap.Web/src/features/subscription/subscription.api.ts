import { api } from "@/lib/api"
import type {
  InitializeSubscriptionResponse,
  Subscription,
  SubscriptionOverview,
  SubscriptionPayment,
} from "./subscription.types"

export const subscriptionApi = {
  getOverview() {
    return api.get<SubscriptionOverview>("/api/subscription/overview")
  },

  getPayments() {
    return api.get<SubscriptionPayment[]>("/api/subscription/payments")
  },

  changePlan(plan: number) {
    return api.post<InitializeSubscriptionResponse, { plan: number }>(
      "/api/subscription/change-plan",
      { plan },
    )
  },

  cancel() {
    return api.postEmpty<Subscription>("/api/subscription/cancel")
  },

  getManageLink() {
    return api.get<{ url: string }>("/api/subscription/manage-link")
  },
}
