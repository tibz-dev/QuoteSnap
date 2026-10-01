export const QuoteStatus = {
  Draft: 1,
  Sent: 2,
  Viewed: 3,
  Accepted: 4,
  Declined: 5,
  Expired: 6,
  ConvertedToInvoice: 7,
  Cancelled: 8,
} as const

export type QuoteItem = {
  id: string
  catalogueItemId: string | null
  description: string
  quantity: number
  unitPrice: number
  discountAmount: number
  lineSubtotal: number
  lineTotal: number
}

export type Quote = {
  id: string
  quoteNumber: string
  customerId: string
  customerName: string
  status: number
  issueDate: string
  validUntil: string
  currencyCode: string
  taxName: string | null
  taxRate: number
  subtotal: number
  discountAmount: number
  taxAmount: number
  total: number
  notes: string | null
  terms: string | null
  createdAt: string
  items: QuoteItem[]
}

export type CreateQuoteItemInput = {
  catalogueItemId: string | null
  description: string | null
  quantity: number
  unitPrice: number | null
  discountAmount: number
}

export type CreateQuoteInput = {
  customerId: string
  validUntil: string | null
  currencyCode: string | null
  taxRate: number | null
  notes: string | null
  terms: string | null
  items: CreateQuoteItemInput[]
}
