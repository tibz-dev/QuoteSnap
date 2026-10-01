import { api } from "@/lib/api"
import type { Notification } from "./notification.types"

export const notificationApi = {
  getRecent(limit = 20) {
    return api.get<Notification[]>(`/api/notifications?limit=${limit}`)
  },
}
