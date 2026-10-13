export const ItemType = {
  Service: 1,
  Product: 2,
} as const

export const PricingType = {
  Fixed: 1,
  PerUnit: 2,
  Hourly: 3,
  Daily: 4,
  Monthly: 5,
  Yearly: 6,
  Custom: 7,
} as const

export type CatalogueItem = {
  id: string
  name: string
  description: string | null
  type: number
  pricingType: number
  defaultPrice: number | null
  unit: string
  isTaxable: boolean
  isActive: boolean
  categoryId: string | null
  categoryName: string | null
  createdAt: string
}

export type Category = {
  id: string
  name: string
  description: string | null
  isActive: boolean
  createdAt: string
}

export type CatalogueItemInput = {
  name: string
  description: string | null
  type: number
  pricingType: number
  defaultPrice: number | null
  unit: string
  isTaxable: boolean
  isActive?: boolean
  categoryId: string | null
}

export type CategoryInput = {
  name: string
  description: string | null
}
