export type Customer = {
  id: string
  name: string
  companyName: string | null
  email: string | null
  phone: string | null
  address: string | null
  taxRegistrationNumber: string | null
  createdAt: string
}

export type CustomerInput = {
  name: string
  companyName: string | null
  email: string | null
  phone: string | null
  address: string | null
  taxRegistrationNumber: string | null
}
