import { useEffect, useMemo, useState, type FormEvent } from "react"
import {
  Boxes,
  BriefcaseBusiness,
  LoaderCircle,
  Package,
  Pencil,
  Plus,
  Search,
  Tag,
  Trash2,
  X,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { catalogueApi } from "./catalogue.api"
import {
  ItemType,
  PricingType,
  type CatalogueItem,
  type CatalogueItemInput,
  type Category,
} from "./catalogue.types"

const pricingLabels: Record<number, string> = {
  [PricingType.Fixed]: "Fixed",
  [PricingType.PerUnit]: "Per unit",
  [PricingType.Hourly]: "Hourly",
  [PricingType.Daily]: "Daily",
  [PricingType.Monthly]: "Monthly",
  [PricingType.Yearly]: "Yearly",
  [PricingType.Custom]: "Custom",
}

const emptyForm: CatalogueItemInput = {
  name: "",
  description: null,
  type: ItemType.Service,
  pricingType: PricingType.Fixed,
  defaultPrice: 0,
  unit: "Unit",
  isTaxable: true,
  isActive: true,
  categoryId: null,
}

export function ServicesPage() {
  const [items, setItems] = useState<CatalogueItem[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [search, setSearch] = useState("")
  const [typeFilter, setTypeFilter] = useState<"all" | "service" | "product">("all")
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [formError, setFormError] = useState("")
  const [editingItem, setEditingItem] = useState<CatalogueItem | null>(null)
  const [form, setForm] = useState<CatalogueItemInput>(emptyForm)
  const [showCategoryCreator, setShowCategoryCreator] = useState(false)
  const [newCategoryName, setNewCategoryName] = useState("")
  const [isCreatingCategory, setIsCreatingCategory] = useState(false)

  useEffect(() => {
    void loadData()
  }, [])

  async function loadData() {
    try {
      setIsLoading(true)
      setError("")
      const [catalogueItems, categoryItems] = await Promise.all([
        catalogueApi.getAll(),
        catalogueApi.getCategories(),
      ])
      setItems(catalogueItems)
      setCategories(categoryItems.filter((category) => category.isActive))
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your service catalogue.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    return items.filter((item) => {
      const matchesType =
        typeFilter === "all" ||
        (typeFilter === "service" && item.type === ItemType.Service) ||
        (typeFilter === "product" && item.type === ItemType.Product)

      const matchesSearch =
        !query ||
        [item.name, item.description, item.categoryName, item.unit]
          .filter(Boolean)
          .some((value) => value!.toLowerCase().includes(query))

      return matchesType && matchesSearch
    })
  }, [items, search, typeFilter])

  const openCreate = () => {
    setEditingItem(null)
    setForm(emptyForm)
    setFormError("")
    setShowCategoryCreator(false)
    setNewCategoryName("")
    setIsFormOpen(true)
  }

  const openEdit = (item: CatalogueItem) => {
    setEditingItem(item)
    setForm({
      name: item.name,
      description: item.description,
      type: item.type,
      pricingType: item.pricingType,
      defaultPrice: item.defaultPrice,
      unit: item.unit,
      isTaxable: item.isTaxable,
      isActive: item.isActive,
      categoryId: item.categoryId,
    })
    setFormError("")
    setShowCategoryCreator(false)
    setNewCategoryName("")
    setIsFormOpen(true)
  }

  const closeForm = () => {
    if (isSaving) return
    setIsFormOpen(false)
    setEditingItem(null)
    setForm(emptyForm)
    setFormError("")
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFormError("")

    if (!form.name.trim()) {
      setFormError("Service or product name is required.")
      return
    }

    if (!form.unit.trim()) {
      setFormError("Unit is required.")
      return
    }

    if (form.defaultPrice !== null && form.defaultPrice < 0) {
      setFormError("Default price cannot be negative.")
      return
    }

    const request: CatalogueItemInput = {
      ...form,
      name: form.name.trim(),
      description: form.description?.trim() || null,
      unit: form.unit.trim(),
      categoryId: form.categoryId || null,
      defaultPrice:
        form.pricingType === PricingType.Custom ? null : form.defaultPrice,
      isActive: editingItem ? form.isActive !== false : undefined,
    }

    try {
      setIsSaving(true)
      const saved = editingItem
        ? await catalogueApi.update(editingItem.id, request)
        : await catalogueApi.create(request)

      setItems((current) =>
        editingItem
          ? current.map((item) => (item.id === saved.id ? saved : item))
          : [saved, ...current],
      )

      setIsFormOpen(false)
      setEditingItem(null)
      setForm(emptyForm)
    } catch (error) {
      setFormError(
        error instanceof ApiError
          ? error.message
          : "We couldn't save this catalogue item.",
      )
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async (item: CatalogueItem) => {
    if (!window.confirm(`Delete ${item.name}? This cannot be undone.`)) return

    try {
      setError("")
      await catalogueApi.delete(item.id)
      setItems((current) => current.filter((entry) => entry.id !== item.id))
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't delete this item.",
      )
    }
  }

  const handleCreateCategory = async () => {
    const name = newCategoryName.trim()
    if (!name) return

    try {
      setIsCreatingCategory(true)
      setFormError("")
      const category = await catalogueApi.createCategory({
        name,
        description: null,
      })
      setCategories((current) => [...current, category].sort((a, b) => a.name.localeCompare(b.name)))
      setForm((current) => ({ ...current, categoryId: category.id }))
      setNewCategoryName("")
      setShowCategoryCreator(false)
    } catch (error) {
      setFormError(
        error instanceof ApiError
          ? error.message
          : "We couldn't create this category.",
      )
    } finally {
      setIsCreatingCategory(false)
    }
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-sm font-semibold text-primary">CATALOGUE</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
              Services & products
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Save the things you bill for so quotes take seconds to create.
            </p>
          </div>
          <button
            type="button"
            onClick={openCreate}
            className="inline-flex h-10 items-center justify-center gap-2 self-start rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground shadow-sm transition hover:opacity-95"
          >
            <Plus className="size-4" />
            Add item
          </button>
        </div>

        <div className="mt-8 rounded-2xl border border-border bg-card">
          <div className="flex flex-col gap-4 border-b border-border p-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="relative w-full lg:max-w-sm">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search catalogue..."
                className="h-10 w-full rounded-xl border border-input bg-background pl-10 pr-4 text-sm outline-none transition placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
              />
            </div>
            <div className="flex gap-2">
              {(["all", "service", "product"] as const).map((filter) => (
                <button
                  key={filter}
                  type="button"
                  onClick={() => setTypeFilter(filter)}
                  className={`rounded-lg px-3 py-2 text-xs font-semibold capitalize transition ${
                    typeFilter === filter
                      ? "bg-accent text-accent-foreground"
                      : "text-muted-foreground hover:bg-muted"
                  }`}
                >
                  {filter}
                </button>
              ))}
            </div>
          </div>

          {error && (
            <div
              className="m-4 rounded-xl px-4 py-3 text-sm"
              style={{
                color: "var(--status-danger)",
                backgroundColor: "var(--status-danger-bg)",
              }}
            >
              {error}
            </div>
          )}

          {isLoading ? (
            <div className="flex min-h-72 items-center justify-center">
              <LoaderCircle className="size-6 animate-spin text-primary" />
            </div>
          ) : filteredItems.length === 0 ? (
            <div className="flex min-h-72 flex-col items-center justify-center px-6 py-12 text-center">
              <span className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
                <Boxes className="size-5" />
              </span>
              <p className="mt-4 text-sm font-semibold">
                {search ? "No matching items" : "Your catalogue is empty"}
              </p>
              <p className="mt-1 max-w-sm text-xs leading-5 text-muted-foreground">
                Add a service or product once, then reuse it on future quotes.
              </p>
              {!search && (
                <button
                  type="button"
                  onClick={openCreate}
                  className="mt-5 inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
                >
                  <Plus className="size-3.5" />
                  Add first item
                </button>
              )}
            </div>
          ) : (
            <div className="grid gap-3 p-4 md:grid-cols-2 xl:grid-cols-3">
              {filteredItems.map((item) => (
                <article
                  key={item.id}
                  className="rounded-2xl border border-border bg-background p-4"
                >
                  <div className="flex items-start justify-between gap-3">
                    <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-accent text-accent-foreground">
                      {item.type === ItemType.Service ? (
                        <BriefcaseBusiness className="size-4.5" />
                      ) : (
                        <Package className="size-4.5" />
                      )}
                    </span>
                    <div className="flex gap-1">
                      <button
                        type="button"
                        onClick={() => openEdit(item)}
                        className="inline-flex size-8 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted hover:text-foreground"
                        aria-label={`Edit ${item.name}`}
                      >
                        <Pencil className="size-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={() => void handleDelete(item)}
                        className="inline-flex size-8 items-center justify-center rounded-lg text-muted-foreground hover:bg-[var(--status-danger-bg)] hover:text-[var(--status-danger)]"
                        aria-label={`Delete ${item.name}`}
                      >
                        <Trash2 className="size-3.5" />
                      </button>
                    </div>
                  </div>

                  <h3 className="mt-4 text-sm font-semibold">{item.name}</h3>
                  <p className="mt-1 line-clamp-2 min-h-10 text-xs leading-5 text-muted-foreground">
                    {item.description || "No description"}
                  </p>

                  <div className="mt-4 flex flex-wrap gap-2 text-[11px]">
                    <span className="rounded-full bg-muted px-2.5 py-1 text-muted-foreground">
                      {item.type === ItemType.Service ? "Service" : "Product"}
                    </span>
                    <span className="rounded-full bg-muted px-2.5 py-1 text-muted-foreground">
                      {pricingLabels[item.pricingType] || "Pricing"}
                    </span>
                    {item.categoryName && (
                      <span className="rounded-full bg-muted px-2.5 py-1 text-muted-foreground">
                        {item.categoryName}
                      </span>
                    )}
                    {!item.isActive && (
                      <span
                        className="rounded-full px-2.5 py-1"
                        style={{
                          color: "var(--status-danger)",
                          backgroundColor: "var(--status-danger-bg)",
                        }}
                      >
                        Inactive
                      </span>
                    )}
                  </div>

                  <div className="mt-4 flex items-end justify-between border-t border-border pt-4">
                    <div>
                      <p className="text-[11px] text-muted-foreground">Default price</p>
                      <p className="mt-1 text-lg font-semibold">
                        {item.defaultPrice === null
                          ? "Custom"
                          : formatMoney(item.defaultPrice)}
                      </p>
                    </div>
                    <p className="text-xs text-muted-foreground">/{item.unit}</p>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>

      {isFormOpen && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/35 sm:items-center sm:p-6">
          <button
            type="button"
            className="absolute inset-0 cursor-default"
            onClick={closeForm}
            aria-label="Close form"
          />
          <div className="relative z-10 max-h-[94vh] w-full max-w-2xl overflow-y-auto rounded-t-3xl border border-border bg-card shadow-2xl sm:rounded-3xl">
            <div className="sticky top-0 z-10 flex items-start justify-between border-b border-border bg-card px-5 py-4 sm:px-6">
              <div>
                <h3 className="text-lg font-semibold">
                  {editingItem ? "Edit catalogue item" : "Add catalogue item"}
                </h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  Set the default details that will flow into quotes.
                </p>
              </div>
              <button
                type="button"
                onClick={closeForm}
                className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted"
              >
                <X className="size-4.5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5 p-5 sm:p-6">
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium">Item type</span>
                  <select
                    value={form.type}
                    onChange={(event) =>
                      setForm((current) => ({ ...current, type: Number(event.target.value) }))
                    }
                    className="h-11 w-full rounded-xl border border-input bg-background px-3 text-sm outline-none focus:border-primary"
                  >
                    <option value={ItemType.Service}>Service</option>
                    <option value={ItemType.Product}>Product</option>
                  </select>
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium">Name</span>
                  <input
                    value={form.name}
                    onChange={(event) =>
                      setForm((current) => ({ ...current, name: event.target.value }))
                    }
                    placeholder="Web Development"
                    className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                  />
                </label>
              </div>

              <label className="block">
                <span className="mb-2 block text-sm font-medium">Description</span>
                <textarea
                  value={form.description || ""}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      description: event.target.value || null,
                    }))
                  }
                  placeholder="What does this service or product include?"
                  rows={3}
                  className="w-full resize-none rounded-xl border border-input bg-background px-4 py-3 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                />
              </label>

              <div className="grid gap-4 sm:grid-cols-3">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium">Pricing</span>
                  <select
                    value={form.pricingType}
                    onChange={(event) => {
                      const pricingType = Number(event.target.value)
                      setForm((current) => ({
                        ...current,
                        pricingType,
                        defaultPrice:
                          pricingType === PricingType.Custom
                            ? null
                            : current.defaultPrice ?? 0,
                      }))
                    }}
                    className="h-11 w-full rounded-xl border border-input bg-background px-3 text-sm outline-none focus:border-primary"
                  >
                    {Object.entries(pricingLabels).map(([value, label]) => (
                      <option key={value} value={value}>{label}</option>
                    ))}
                  </select>
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium">Default price</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    disabled={form.pricingType === PricingType.Custom}
                    value={form.defaultPrice ?? ""}
                    onChange={(event) =>
                      setForm((current) => ({
                        ...current,
                        defaultPrice: event.target.value === "" ? null : Number(event.target.value),
                      }))
                    }
                    placeholder="0.00"
                    className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none disabled:bg-muted disabled:text-muted-foreground focus:border-primary"
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium">Unit</span>
                  <input
                    value={form.unit}
                    onChange={(event) =>
                      setForm((current) => ({ ...current, unit: event.target.value }))
                    }
                    placeholder="Hour"
                    className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none placeholder:text-muted-foreground/70 focus:border-primary"
                  />
                </label>
              </div>

              <div>
                <div className="mb-2 flex items-center justify-between">
                  <span className="text-sm font-medium">Category</span>
                  <button
                    type="button"
                    onClick={() => setShowCategoryCreator((current) => !current)}
                    className="text-xs font-semibold text-primary hover:underline"
                  >
                    + New category
                  </button>
                </div>
                <select
                  value={form.categoryId || ""}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      categoryId: event.target.value || null,
                    }))
                  }
                  className="h-11 w-full rounded-xl border border-input bg-background px-3 text-sm outline-none focus:border-primary"
                >
                  <option value="">No category</option>
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>

                {showCategoryCreator && (
                  <div className="mt-3 flex gap-2 rounded-xl bg-muted p-3">
                    <div className="relative flex-1">
                      <Tag className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                      <input
                        value={newCategoryName}
                        onChange={(event) => setNewCategoryName(event.target.value)}
                        placeholder="e.g. Design"
                        className="h-10 w-full rounded-lg border border-input bg-card pl-9 pr-3 text-sm outline-none focus:border-primary"
                      />
                    </div>
                    <button
                      type="button"
                      disabled={isCreatingCategory || !newCategoryName.trim()}
                      onClick={() => void handleCreateCategory()}
                      className="inline-flex h-10 items-center gap-2 rounded-lg bg-primary px-3 text-xs font-semibold text-primary-foreground disabled:opacity-50"
                    >
                      {isCreatingCategory && <LoaderCircle className="size-3.5 animate-spin" />}
                      Add
                    </button>
                  </div>
                )}
              </div>

              <div className="flex flex-wrap gap-5 rounded-xl bg-muted p-4">
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={form.isTaxable}
                    onChange={(event) =>
                      setForm((current) => ({ ...current, isTaxable: event.target.checked }))
                    }
                    className="size-4 accent-[var(--primary)]"
                  />
                  Taxable
                </label>

                {editingItem && (
                  <label className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={form.isActive !== false}
                      onChange={(event) =>
                        setForm((current) => ({ ...current, isActive: event.target.checked }))
                      }
                      className="size-4 accent-[var(--primary)]"
                    />
                    Active
                  </label>
                )}
              </div>

              {formError && (
                <div
                  className="rounded-xl px-4 py-3 text-sm"
                  style={{
                    color: "var(--status-danger)",
                    backgroundColor: "var(--status-danger-bg)",
                  }}
                >
                  {formError}
                </div>
              )}

              <div className="flex flex-col-reverse gap-3 border-t border-border pt-5 sm:flex-row sm:justify-end">
                <button
                  type="button"
                  onClick={closeForm}
                  disabled={isSaving}
                  className="h-10 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-muted disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground disabled:opacity-60"
                >
                  {isSaving && <LoaderCircle className="size-4 animate-spin" />}
                  {editingItem ? "Save changes" : "Add item"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  )
}

function formatMoney(amount: number) {
  return new Intl.NumberFormat("en-ZA", {
    style: "currency",
    currency: "ZAR",
  }).format(amount)
}
