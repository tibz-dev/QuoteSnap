import { api } from "@/lib/api"
import type { CreateQuoteInput, Quote } from "./quote.types"

export const quoteApi = {
  getAll() {
    return api.get<Quote[]>("/api/quotes")
  },

  create(request: CreateQuoteInput) {
    return api.post<Quote, CreateQuoteInput>("/api/quotes", request)
  },

  updateStatus(id: string, status: number) {
    return api.patch<Quote, { status: number }>(
      `/api/quotes/${id}/status`,
      { status },
    )
  },

  downloadPdf(id: string) {
    return api.download(`/api/quotes/${id}/pdf`)
  },
}
