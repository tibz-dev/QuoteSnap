import { api } from "@/lib/api"
import { customerApi } from "@/features/customers/customer.api"
import type { Customer } from "@/features/customers/customer.types"

export type DashboardInvoice = {
  id: string
  invoiceNumber: string
  customerId: string
  customerName: string
  status: number
  issueDate: string
  dueDate: string
  currencyCode: string
  total: number
  amountPaid: number
  balanceDue: number
  createdAt: string
}

export type DashboardData = {
  invoices: DashboardInvoice[]
  customers: Customer[]
}

export async function getDashboardData(): Promise<DashboardData> {
  const [invoices, customers] = await Promise.all([
    api.get<DashboardInvoice[]>("/api/invoices"),
    customerApi.getAll(),
  ])

  return {
    invoices,
    customers,
  }
}
