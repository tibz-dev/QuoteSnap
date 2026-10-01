import { api } from "@/lib/api"
import type { Customer, CustomerInput } from "./customer.types"

export const customerApi = {
  getAll() {
    return api.get<Customer[]>("/api/customers")
  },

  create(request: CustomerInput) {
    return api.post<Customer, CustomerInput>("/api/customers", request)
  },

  update(id: string, request: CustomerInput) {
    return api.put<Customer, CustomerInput>(
      `/api/customers/${id}`,
      request,
    )
  },

  delete(id: string) {
    return api.delete(`/api/customers/${id}`)
  },
}
