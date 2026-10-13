export type Receipt = {
  id: string
  receiptNumber: string
  paymentId: string
  invoiceId: string
  invoiceNumber: string
  customerId: string
  customerName: string
  receiptDate: string
  currencyCode: string
  amount: number
  paymentMethod: number
  paymentDate: string
  paymentReference: string | null
  paymentNotes: string | null
  createdAt: string
}
