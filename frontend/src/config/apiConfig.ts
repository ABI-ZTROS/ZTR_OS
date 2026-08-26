declare global {
  interface Window {
    __API_BASE_URL__?: string
  }
}

export function getApiBaseUrl(): string {
  if (typeof window !== 'undefined' && window.__API_BASE_URL__) {
    return window.__API_BASE_URL__
  }
  return import.meta.env.VITE_API_URL || 'http://localhost:5000'
}

export function getHubUrl(hubName: string): string {
  const base = getApiBaseUrl().replace(/\/$/, '')
  return `${base}/hubs/${hubName}`
}

export function getApiUrl(path: string): string {
  const base = getApiBaseUrl().replace(/\/$/, '')
  return path.startsWith('http') ? path : `${base}${path}`
}

export function isDesktopMode(): boolean {
  if (typeof window === 'undefined') return false
  return !!window.__API_BASE_URL__
}
