import {
  clearAuthSession,
  getAuthSession,
} from "@/features/auth/auth.storage"

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

function createHeaders(init: RequestInit = {}) {
  const session = getAuthSession()
  const headers = new Headers(init.headers)

  headers.set("Accept", "application/json")

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json")
  }

  if (session?.token) {
    headers.set("Authorization", `Bearer ${session.token}`)
  }

  return headers
}

function handleUnauthorizedResponse(response: Response) {
  if (response.status !== 401 || !getAuthSession()) {
    return
  }

  clearAuthSession()
  window.dispatchEvent(new Event("quotesnap:unauthorized"))
}

async function getErrorMessage(response: Response) {
  const rawBody = await response.text()

  if (!rawBody) {
    return response.status === 401
      ? "Your session has expired. Please sign in again."
      : "Something went wrong. Please try again."
  }

  try {
    const payload = JSON.parse(rawBody) as unknown

    if (
      typeof payload === "object" &&
      payload !== null &&
      "message" in payload &&
      typeof payload.message === "string"
    ) {
      return payload.message
    }
  } catch {
    return rawBody
  }

  return "Something went wrong. Please try again."
}

async function request<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: createHeaders(init),
  })

  if (!response.ok) {
    handleUnauthorizedResponse(response)
    throw new ApiError(await getErrorMessage(response), response.status)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const rawBody = await response.text()

  if (!rawBody) {
    return undefined as T
  }

  try {
    return JSON.parse(rawBody) as T
  } catch {
    return rawBody as unknown as T
  }
}

async function download(path: string) {
  const headers = createHeaders()
  headers.set("Accept", "application/pdf")

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "GET",
    headers,
  })

  if (!response.ok) {
    handleUnauthorizedResponse(response)
    throw new ApiError(await getErrorMessage(response), response.status)
  }

  const disposition = response.headers.get("content-disposition")
  const fileNameMatch = disposition?.match(/filename="?([^";]+)"?/i)

  return {
    blob: await response.blob(),
    fileName: fileNameMatch?.[1] || "document.pdf",
  }
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

  postEmpty<TResponse>(path: string) {
    return request<TResponse>(path, {
      method: "POST",
    })
  },

  put<TResponse, TBody>(path: string, body: TBody) {
    return request<TResponse>(path, {
      method: "PUT",
      body: JSON.stringify(body),
    })
  },

  patch<TResponse, TBody>(path: string, body: TBody) {
    return request<TResponse>(path, {
      method: "PATCH",
      body: JSON.stringify(body),
    })
  },

  delete(path: string) {
    return request<void>(path, {
      method: "DELETE",
    })
  },

  download,
}
