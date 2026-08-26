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
    const curvesRes = await api.get<{ cpu: number[]; gpu: number[]; mid: number[] }>('/api/performance/fan-curves')
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
  getPowerLimit: () => api.get<PowerLimitsResponse>('/api/performance/power-limits'),
  setPowerLimit: (component: 'cpu' | 'gpu' | 'spl' | 'sppt' | 'fppt', limit: number) =>
    MapPowerLimit(component, limit),
  setAllPowerLimits: (spl: number, sppt: number, fppt: number) =>
    api.post<void>('/api/performance/power-limits', { spl, sppt, fppt }),
  getGpuMode: () => api.get<{ mode: string }>('/api/performance/mode'),
  setGpuMode: (mode: string) =>
    api.post<void>('/api/performance/mode', { mode }),
  getFanCurves: () => api.get<{ cpu: number[]; gpu: number[]; mid: number[] }>('/api/performance/fan-curves'),
  setFanCurve: (fanId: number, curve: number[]) =>
    api.post<void>('/api/performance/fan-curves', { device: fanId, curve }),
}

function MapPowerLimit(component: string, limit: number) {
  switch (component) {
    case 'cpu':
      return api.post<void>('/api/performance/cpu-power', { watts: limit })
    case 'gpu':
      return api.post<void>('/api/performance/gpu-power', { watts: limit })
    case 'spl':
      return api.post<void>('/api/performance/power-limits', { spl: limit, sppt: null, fppt: null })
    case 'sppt':
      return api.post<void>('/api/performance/power-limits', { spl: null, sppt: limit, fppt: null })
    case 'fppt':
      return api.post<void>('/api/performance/power-limits', { spl: null, sppt: null, fppt: limit })
    default:
      return api.post<void>('/api/performance/power-limits', { spl: limit, sppt: limit, fppt: limit })
  }
}