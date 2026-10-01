import { api } from "@/lib/api"
import type { Invoice } from "@/features/invoices/invoice.types"
import type { Payment, RecordPaymentInput } from "./payment.types"

export const paymentApi = {
  getForInvoice(invoiceId: string) {
    return api.get<Payment[]>(`/api/invoices/${invoiceId}/payments`)
  },

  record(invoiceId: string, request: RecordPaymentInput) {
    return api.post<Invoice, RecordPaymentInput>(
      `/api/invoices/${invoiceId}/payments`,
      request,
    )
  },
}
