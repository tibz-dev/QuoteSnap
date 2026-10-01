import { useEffect, useMemo, useState, type FormEvent } from "react"
import {
  Building2,
  LoaderCircle,
  Mail,
  MapPin,
  Pencil,
  Phone,
  Plus,
  Search,
  Trash2,
  UserRound,
  X,
} from "lucide-react"
import { ApiError } from "@/lib/api"
import { customerApi } from "./customer.api"
import type { Customer, CustomerInput } from "./customer.types"

const emptyForm: CustomerInput = {
  name: "",
  companyName: null,
  email: null,
  phone: null,
  address: null,
  taxRegistrationNumber: null,
}

export function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [search, setSearch] = useState("")
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState("")
  const [formError, setFormError] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null)
  const [form, setForm] = useState<CustomerInput>(emptyForm)

  useEffect(() => {
    void loadCustomers()
  }, [])

  async function loadCustomers() {
    try {
      setIsLoading(true)
      setError("")
      const result = await customerApi.getAll()
      setCustomers(result)
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't load your customers.",
      )
    } finally {
      setIsLoading(false)
    }
  }

  const filteredCustomers = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) {
      return customers
    }

    return customers.filter((customer) =>
      [
        customer.name,
        customer.companyName,
        customer.email,
        customer.phone,
      ]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query)),
    )
  }, [customers, search])

  const openCreateForm = () => {
    setEditingCustomer(null)
    setForm(emptyForm)
    setFormError("")
    setIsFormOpen(true)
  }

  const openEditForm = (customer: Customer) => {
    setEditingCustomer(customer)
    setForm({
      name: customer.name,
      companyName: customer.companyName,
      email: customer.email,
      phone: customer.phone,
      address: customer.address,
      taxRegistrationNumber: customer.taxRegistrationNumber,
    })
    setFormError("")
    setIsFormOpen(true)
  }

  const closeForm = () => {
    if (isSaving) {
      return
    }

    setIsFormOpen(false)
    setEditingCustomer(null)
    setForm(emptyForm)
    setFormError("")
  }

  const setField = (field: keyof CustomerInput, value: string) => {
    setForm((current) => ({
      ...current,
      [field]: value.trimStart() || (field === "name" ? "" : null),
    }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFormError("")

    if (!form.name.trim()) {
      setFormError("Customer name is required.")
      return
    }

    const request: CustomerInput = {
      name: form.name.trim(),
      companyName: form.companyName?.trim() || null,
      email: form.email?.trim() || null,
      phone: form.phone?.trim() || null,
      address: form.address?.trim() || null,
      taxRegistrationNumber: form.taxRegistrationNumber?.trim() || null,
    }

    try {
      setIsSaving(true)

      const savedCustomer = editingCustomer
        ? await customerApi.update(editingCustomer.id, request)
        : await customerApi.create(request)

      setCustomers((current) =>
        editingCustomer
          ? current.map((customer) =>
              customer.id === savedCustomer.id ? savedCustomer : customer,
            )
          : [savedCustomer, ...current],
      )

      setIsFormOpen(false)
      setEditingCustomer(null)
      setForm(emptyForm)
      setFormError("")
    } catch (error) {
      setFormError(
        error instanceof ApiError
          ? error.message
          : "We couldn't save this customer.",
      )
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async (customer: Customer) => {
    const confirmed = window.confirm(
      `Delete ${customer.name}? This action cannot be undone.`,
    )

    if (!confirmed) {
      return
    }

    try {
      setError("")
      await customerApi.delete(customer.id)
      setCustomers((current) =>
        current.filter((item) => item.id !== customer.id),
      )
    } catch (error) {
      setError(
        error instanceof ApiError
          ? error.message
          : "We couldn't delete this customer.",
      )
    }
  }

  return (
    <section className="px-4 py-7 sm:px-6 lg:px-8 lg:py-9">
      <div className="mx-auto max-w-[1500px]">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-sm font-semibold text-primary">CUSTOMERS</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.04em]">
              Your customer book
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Keep billing details ready for quotes, invoices and receipts.
            </p>
          </div>

          <button
            type="button"
            onClick={openCreateForm}
            className="inline-flex h-10 items-center justify-center gap-2 self-start rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground shadow-sm transition hover:opacity-95"
          >
            <Plus className="size-4" />
            Add customer
          </button>
        </div>

        <div className="mt-8 rounded-2xl border border-border bg-card">
          <div className="flex flex-col gap-4 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative w-full sm:max-w-sm">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search customers..."
                className="h-10 w-full rounded-xl border border-input bg-background pl-10 pr-4 text-sm outline-none transition placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
              />
            </div>

            <p className="text-xs text-muted-foreground">
              {customers.length} {customers.length === 1 ? "customer" : "customers"}
            </p>
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
          ) : filteredCustomers.length === 0 ? (
            <div className="flex min-h-72 flex-col items-center justify-center px-6 py-12 text-center">
              <span className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
                <UserRound className="size-5" />
              </span>
              <p className="mt-4 text-sm font-semibold">
                {search ? "No matching customers" : "No customers yet"}
              </p>
              <p className="mt-1 max-w-sm text-xs leading-5 text-muted-foreground">
                {search
                  ? "Try another name, company, email or phone number."
                  : "Add your first customer so you can start creating quotes and invoices."}
              </p>
              {!search && (
                <button
                  type="button"
                  onClick={openCreateForm}
                  className="mt-5 inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-xs font-semibold text-primary-foreground"
                >
                  <Plus className="size-3.5" />
                  Add customer
                </button>
              )}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[820px] border-collapse text-left">
                <thead>
                  <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                    <th className="px-5 py-3 font-medium">Customer</th>
                    <th className="px-5 py-3 font-medium">Contact</th>
                    <th className="px-5 py-3 font-medium">Location</th>
                    <th className="px-5 py-3 font-medium">Added</th>
                    <th className="px-5 py-3 text-right font-medium">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredCustomers.map((customer) => (
                    <tr
                      key={customer.id}
                      className="border-b border-border last:border-0 hover:bg-muted/30"
                    >
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-accent text-accent-foreground">
                            {customer.companyName ? (
                              <Building2 className="size-4.5" />
                            ) : (
                              <UserRound className="size-4.5" />
                            )}
                          </span>
                          <div>
                            <p className="text-sm font-semibold">{customer.name}</p>
                            <p className="mt-0.5 text-xs text-muted-foreground">
                              {customer.companyName || "Individual customer"}
                            </p>
                          </div>
                        </div>
                      </td>

                      <td className="px-5 py-4">
                        <div className="space-y-1.5 text-xs">
                          <p className="flex items-center gap-2 text-foreground">
                            <Mail className="size-3.5 text-muted-foreground" />
                            {customer.email || "No email"}
                          </p>
                          <p className="flex items-center gap-2 text-muted-foreground">
                            <Phone className="size-3.5" />
                            {customer.phone || "No phone"}
                          </p>
                        </div>
                      </td>

                      <td className="max-w-[240px] px-5 py-4">
                        <p className="flex items-start gap-2 text-xs text-muted-foreground">
                          <MapPin className="mt-0.5 size-3.5 shrink-0" />
                          <span className="line-clamp-2">
                            {customer.address || "No address"}
                          </span>
                        </p>
                      </td>

                      <td className="px-5 py-4 text-xs text-muted-foreground">
                        {new Intl.DateTimeFormat("en-ZA", {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                        }).format(new Date(customer.createdAt))}
                      </td>

                      <td className="px-5 py-4">
                        <div className="flex justify-end gap-1">
                          <button
                            type="button"
                            onClick={() => openEditForm(customer)}
                            className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground transition hover:bg-accent hover:text-accent-foreground"
                            aria-label={`Edit ${customer.name}`}
                          >
                            <Pencil className="size-4" />
                          </button>
                          <button
                            type="button"
                            onClick={() => void handleDelete(customer)}
                            className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground transition hover:bg-[var(--status-danger-bg)] hover:text-[var(--status-danger)]"
                            aria-label={`Delete ${customer.name}`}
                          >
                            <Trash2 className="size-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {isFormOpen && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/35 p-0 sm:items-center sm:p-6">
          <button
            type="button"
            onClick={closeForm}
            className="absolute inset-0 cursor-default"
            aria-label="Close customer form"
          />

          <div className="relative z-10 max-h-[92vh] w-full max-w-xl overflow-y-auto rounded-t-3xl border border-border bg-card shadow-2xl sm:rounded-3xl">
            <div className="sticky top-0 flex items-start justify-between border-b border-border bg-card px-5 py-4 sm:px-6">
              <div>
                <h3 className="text-lg font-semibold">
                  {editingCustomer ? "Edit customer" : "Add customer"}
                </h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  These details will appear on business documents where needed.
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
              <CustomerField
                label="Customer name"
                value={form.name}
                onChange={(value) => setField("name", value)}
                placeholder="John Mokoena"
                required
              />

              <CustomerField
                label="Company name"
                value={form.companyName || ""}
                onChange={(value) => setField("companyName", value)}
                placeholder="Mokoena Trading"
              />

              <div className="grid gap-4 sm:grid-cols-2">
                <CustomerField
                  label="Email"
                  value={form.email || ""}
                  onChange={(value) => setField("email", value)}
                  placeholder="john@company.co.za"
                  type="email"
                />
                <CustomerField
                  label="Phone"
                  value={form.phone || ""}
                  onChange={(value) => setField("phone", value)}
                  placeholder="+27 71 234 5678"
                />
              </div>

              <CustomerField
                label="Address"
                value={form.address || ""}
                onChange={(value) => setField("address", value)}
                placeholder="Business or billing address"
              />

              <CustomerField
                label="Tax / VAT number"
                value={form.taxRegistrationNumber || ""}
                onChange={(value) => setField("taxRegistrationNumber", value)}
                placeholder="Optional"
              />

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
                  className="h-10 rounded-xl border border-border px-4 text-sm font-semibold transition hover:bg-muted disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground transition hover:opacity-95 disabled:opacity-60"
                >
                  {isSaving && <LoaderCircle className="size-4 animate-spin" />}
                  {editingCustomer ? "Save changes" : "Add customer"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  )
}

type CustomerFieldProps = {
  label: string
  value: string
  onChange: (value: string) => void
  placeholder: string
  type?: string
  required?: boolean
}

function CustomerField({
  label,
  value,
  onChange,
  placeholder,
  type = "text",
  required = false,
}: CustomerFieldProps) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium">{label}</span>
      <input
        type={type}
        required={required}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="h-11 w-full rounded-xl border border-input bg-background px-4 text-sm outline-none transition placeholder:text-muted-foreground/70 focus:border-primary focus:ring-3 focus:ring-primary/10"
      />
    </label>
  )
}
