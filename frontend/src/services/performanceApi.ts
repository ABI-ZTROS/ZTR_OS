import { api } from './api'

export const performanceApi = {
  getConfig: () => api.get<Record<string, unknown>>('/api/performance/config'),
  setConfig: (config: Record<string, unknown>) =>
    api.put<void>('/api/performance/config', config),
  getPowerLimit: () => api.get<{ cpu: number; gpu: number }>('/api/performance/power-limit'),
  setPowerLimit: (component: 'cpu' | 'gpu', limit: number) =>
    api.post<void>(`/api/performance/power-limit/${component}`, { limit }),
  getGpuMode: () => api.get<{ mode: string }>('/api/performance/gpu-mode'),
  setGpuMode: (mode: string) =>
    api.post<void>('/api/performance/gpu-mode', { mode }),
  getFanCurves: () => api.get<Record<string, number[]>>('/api/performance/fan-curves'),
  setFanCurve: (fanId: number, curve: number[]) =>
    api.put<void>(`/api/performance/fan-curves/${fanId}`, { curve }),
}