import type { ApiResponse } from '@/types'
import { getApiUrl } from '@/config/apiConfig'

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const url = getApiUrl(path)

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    ...(options.headers as Record<string, string>),
  }

  try {
    const response = await fetch(url, {
      ...options,
      headers,
    })

    const text = await response.text()
    const parsed = text ? JSON.parse(text) : null

    if (!response.ok) {
      const message = parsed?.message || `HTTP ${response.status}`
      return { success: false, data: defaultData<T>(), message }
    }

    if (parsed && typeof parsed === 'object' && 'success' in parsed) {
      return parsed as ApiResponse<T>
    }

    return { success: true, data: parsed as T }
  } catch (err) {
    const message = err instanceof Error ? err.message : 'Unknown error'
    return { success: false, data: defaultData<T>(), message }
  }
}

function defaultData<T>(): T {
  return null as unknown as T
}

export const api = {
  get: <T>(path: string) => apiRequest<T>(path, { method: 'GET' }),
  post: <T>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    }),
  put: <T>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    }),
  delete: <T>(path: string) => apiRequest<T>(path, { method: 'DELETE' }),
}
