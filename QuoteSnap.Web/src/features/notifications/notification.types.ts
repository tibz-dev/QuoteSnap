export type Notification = {
  id: string
  type: number
  status: number
  recipient: string
  subject: string
  scheduledFor: string
  sentAt: string | null
  failedAt: string | null
  errorMessage: string | null
  referenceType: string | null
  referenceId: string | null
  createdAt: string
}
