export type Subscription = {
  id: string
  plan: number
  status: number
  trialStartedAt: string | null
  trialEndsAt: string | null
  trialDaysRemaining: number | null
  currentPeriodStartsAt: string | null
  currentPeriodEndsAt: string | null
  cancelledAt: string | null
  endedAt: string | null
}

export type SubscriptionPlanDefinition = {
  plan: number
  name: string
  monthlyPrice: number
  currencyCode: string
  customerLimit: number | null
  catalogueItemLimit: number | null
  monthlyQuoteLimit: number | null
  monthlyInvoiceLimit: number | null
  monthlyReceiptLimit: number | null
  emailDelivery: boolean
  pdfDocuments: boolean
  paymentsAndReceipts: boolean
}

export type SubscriptionUsage = {
  customers: number
  catalogueItems: number
  quotesThisMonth: number
  invoicesThisMonth: number
  receiptsThisMonth: number
}

export type SubscriptionOverview = {
  subscription: Subscription
  effectivePlan: SubscriptionPlanDefinition
  plans: SubscriptionPlanDefinition[]
  usage: SubscriptionUsage
  canWrite: boolean
  isReadOnly: boolean
  isTrialUsingProAccess: boolean
  accessMessage: string | null
}

export type SubscriptionPayment = {
  id: string
  plan: number
  amount: number
  currencyCode: string
  status: number
  reference: string | null
  paidAt: string | null
  failedAt: string | null
  failureReason: string | null
  createdAt: string
}

export type InitializeSubscriptionResponse = {
  paymentId: string
  reference: string
  authorizationUrl: string
  accessCode: string
}
