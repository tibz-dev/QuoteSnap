import { getAuthSession } from "@/features/auth/auth.storage"

export class ApiError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = "ApiError"
    this.status = status
  }
}

const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const API_BASE_URL = configuredBaseUrl
  ? configuredBaseUrl.replace(/\/$/, "")
  : ""

async function request<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const session = getAuthSession()

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init.body ? { "Content-Type": "application/json" } : {}),
      ...(session?.token
        ? { Authorization: `Bearer ${session.token}` }
        : {}),
      ...init.headers,
    },
  })

  const rawBody = await response.text()
  let payload: unknown = null

  if (rawBody) {
    try {
      payload = JSON.parse(rawBody)
    } catch {
      payload = rawBody
    }
  }

  if (!response.ok) {
    const message =
      typeof payload === "object" &&
      payload !== null &&
      "message" in payload &&
      typeof payload.message === "string"
        ? payload.message
        : response.status === 401
          ? "Your session has expired. Please sign in again."
          : "Something went wrong. Please try again."

    throw new ApiError(message, response.status)
  }

  return payload as T
}

export const api = {
  get<TResponse>(path: string) {
    return request<TResponse>(path)
  },

  post<TResponse, TBody>(path: string, body: TBody) {
    return request<TResponse>(path, {
      method: "POST",
      body: JSON.stringify(body),
    })
  },

  put<TResponse, TBody>(path: string, body: TBody) {
    return request<TResponse>(path, {
      method: "PUT",
      body: JSON.stringify(body),
    })
  },

  delete(path: string) {
    return request<void>(path, {
      method: "DELETE",
    })
  },
}
