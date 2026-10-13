import { api } from "@/lib/api"
import type { Receipt } from "./receipt.types"

export const receiptApi = {
  getAll() {
    return api.get<Receipt[]>("/api/receipts")
  },

  generateFromPayment(paymentId: string) {
    return api.postEmpty<Receipt>(`/api/receipts/from-payment/${paymentId}`)
  },

  downloadPdf(id: string) {
    return api.download(`/api/receipts/${id}/pdf`)
  },
}
