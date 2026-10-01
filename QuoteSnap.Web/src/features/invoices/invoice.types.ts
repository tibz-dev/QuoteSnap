export const InvoiceStatus = {
  Draft: 1,
  Sent: 2,
  PartiallyPaid: 3,
  Paid: 4,
  Overdue: 5,
  Cancelled: 6,
} as const

export type InvoiceItem = {
  id: string
  catalogueItemId: string | null
  description: string
  quantity: number
  unitPrice: number
  discountAmount: number
  lineSubtotal: number
  lineTotal: number
}

export type Invoice = {
  id: string
  invoiceNumber: string
  customerId: string
  customerName: string
  quoteId: string | null
  status: number
  issueDate: string
  dueDate: string
  currencyCode: string
  taxName: string | null
  taxRate: number
  subtotal: number
  discountAmount: number
  taxAmount: number
  total: number
  amountPaid: number
  balanceDue: number
  notes: string | null
  terms: string | null
  createdAt: string
  items: InvoiceItem[]
}

export type ConvertQuoteToInvoiceInput = {
  dueDate: string | null
}
