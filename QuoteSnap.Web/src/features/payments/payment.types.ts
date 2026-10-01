export const PaymentMethod = {
  Cash: 1,
  BankTransfer: 2,
  Card: 3,
  MobileMoney: 4,
  Other: 5,
} as const

export type Payment = {
  id: string
  invoiceId: string
  amount: number
  method: number
  paymentDate: string
  reference: string | null
  notes: string | null
  createdAt: string
}

export type RecordPaymentInput = {
  amount: number
  method: number
  paymentDate: string | null
  reference: string | null
  notes: string | null
}
