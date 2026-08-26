import { api } from './api'

export const screenApi = {
  getRefreshRate: () => api.get<{ current: number; supported: number[] }>('/api/screen/refresh-rate'),
  setRefreshRate: (rate: number) => api.post<boolean>('/api/screen/refresh-rate', { rate }),
  getOverdrive: () => api.get<{ enabled: boolean }>('/api/screen/overdrive'),
  setOverdrive: (enabled: boolean) => api.post<boolean>('/api/screen/overdrive', { enabled }),
  getMiniLed: () => api.get<{ mode: string }>('/api/screen/mini-led'),
  setMiniLed: (mode: string) => api.post<boolean>('/api/screen/mini-led', { mode }),
  getHdr: () => api.get<{ enabled: boolean }>('/api/screen/hdr'),
  setHdr: (enabled: boolean) => api.post<boolean>('/api/screen/hdr', { enabled }),
  getOptimalBrightness: () => api.get<{ enabled: boolean }>('/api/screen/optimal-brightness'),
  setOptimalBrightness: (enabled: boolean) => api.post<boolean>('/api/screen/optimal-brightness', { enabled }),
  getKeyboardBrightness: () => api.get<{ level: number }>('/api/screen/keyboard-brightness'),
  setKeyboardBrightness: (level: number) => api.post<boolean>('/api/screen/keyboard-brightness', { level }),
}