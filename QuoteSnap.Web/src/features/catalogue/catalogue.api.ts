import { api } from "@/lib/api"
import type {
  CatalogueItem,
  CatalogueItemInput,
  Category,
  CategoryInput,
} from "./catalogue.types"

export const catalogueApi = {
  getAll() {
    return api.get<CatalogueItem[]>("/api/catalogue-items")
  },

  getCategories() {
    return api.get<Category[]>("/api/categories")
  },

  create(request: CatalogueItemInput) {
    return api.post<CatalogueItem, CatalogueItemInput>(
      "/api/catalogue-items",
      request,
    )
  },

  update(id: string, request: CatalogueItemInput) {
    return api.put<CatalogueItem, CatalogueItemInput>(
      `/api/catalogue-items/${id}`,
      request,
    )
  },

  delete(id: string) {
    return api.delete(`/api/catalogue-items/${id}`)
  },

  createCategory(request: CategoryInput) {
    return api.post<Category, CategoryInput>("/api/categories", request)
  },
}
