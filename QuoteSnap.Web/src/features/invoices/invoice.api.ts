import { api } from "@/lib/api"
import type {
  ConvertQuoteToInvoiceInput,
  Invoice,
  SendInvoiceEmailInput,
  SendInvoiceEmailResponse,
} from "./invoice.types"

export const invoiceApi = {
  getAll() {
    return api.get<Invoice[]>("/api/invoices")
  },

  getById(id: string) {
    return api.get<Invoice>(`/api/invoices/${id}`)
  },

  convertQuote(quoteId: string, request: ConvertQuoteToInvoiceInput) {
    return api.post<Invoice, ConvertQuoteToInvoiceInput>(
      `/api/invoices/from-quote/${quoteId}`,
      request,
    )
  },

  downloadPdf(id: string) {
    return api.download(`/api/invoices/${id}/pdf`)
  },

  sendEmail(id: string, request: SendInvoiceEmailInput) {
    return api.post<SendInvoiceEmailResponse, SendInvoiceEmailInput>(
      `/api/invoices/${id}/send-email`,
      request,
    )
  },
}
