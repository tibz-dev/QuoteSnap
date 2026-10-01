export type BusinessSettings = {
  id: string
  name: string
  email: string | null
  phone: string | null
  address: string | null
  countryCode: string
  currencyCode: string
  isTaxRegistered: boolean
  taxName: string | null
  taxRegistrationNumber: string | null
  defaultTaxRate: number | null
  bankName: string | null
  accountHolder: string | null
  accountNumber: string | null
  branchCode: string | null
  quotePrefix: string
  defaultQuoteValidityDays: number
  invoicePrefix: string
  receiptPrefix: string
}

export type BusinessSettingsInput = Omit<BusinessSettings, "id">

export type EmailConnection = {
  id: string
  provider: number
  emailAddress: string
  isActive: boolean
  connectedAt: string
  lastUsedAt: string | null
}

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

export type InitializeSubscriptionResponse = {
  paymentId: string
  reference: string
  authorizationUrl: string
  accessCode: string
}
