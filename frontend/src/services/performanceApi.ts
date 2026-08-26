import { api } from './api'

export interface PowerLimitsResponse {
  cpu: number
  gpu: number
  spl: number
  sppt: number
  fppt: number
}

export const performanceApi = {
  getConfig: async () => {
    const modeRes = await api.get<{ mode: string }>('/api/performance/mode')
    const curvesRes = await api.get<Record<string, number[]>>('/api/performance/fan-curves')
    return {
      success: modeRes.success && curvesRes.success,
      data: {
        mode: modeRes.data,
        curves: curvesRes.data,
      },
    }
  },
  setConfig: (config: Record<string, unknown>) =>
    api.put<void>('/api/settings', config),
  getPowerLimit: () => api.get<PowerLimitsResponse>('/api/performance/mode'),
  setPowerLimit: (component: 'cpu' | 'gpu' | 'spl' | 'sppt' | 'fppt', limit: number) =>
    api.post<void>('/api/performance/power-limits', MapPowerLimit(component, limit)),
  setAllPowerLimits: (spl: number, sppt: number, fppt: number) =>
    api.post<void>('/api/performance/power-limits', { spl, sppt, fppt }),
  getGpuMode: () => api.get<{ mode: string }>('/api/performance/mode'),
  setGpuMode: (mode: string) =>
    api.post<void>('/api/performance/mode', { mode }),
  getFanCurves: () => api.get<Record<string, number[]>>('/api/performance/fan-curves'),
  setFanCurve: (fanId: number, curve: number[]) =>
    api.post<void>('/api/performance/fan-curves', { device: fanId, curve }),
}

function MapPowerLimit(component: string, limit: number) {
  switch (component) {
    case 'spl':
      return { spl: limit, sppt: null, fppt: null }
    case 'sppt':
      return { spl: null, sppt: limit, fppt: null }
    case 'fppt':
      return { spl: null, sppt: null, fppt: limit }
    default:
      return { spl: limit, sppt: limit, fppt: limit }
  }
}