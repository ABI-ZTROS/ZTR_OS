import { api } from './api'

export const bindingApi = {
  getBindings: () => api.get<Array<Record<string, unknown>>>('/api/binding'),
  setBinding: (processId: number, affinity: number[]) =>
    api.post<void>(`/api/binding/${processId}`, { affinity }),
  removeBinding: (processId: number) =>
    api.delete<void>(`/api/binding/${processId}`),
  getProcesses: () => api.get<Array<Record<string, unknown>>>('/api/binding/processes'),
  getTopology: () => api.get<Array<Record<string, unknown>>>('/api/binding/topology'),
  setAutoBindGames: (enabled: boolean) =>
    api.post<void>('/api/binding/auto-bind', { enabled }),
  setGpuAffinity: (processId: number, gpuIndex: number) =>
    api.post<void>('/api/binding/gpu', { processId, gpuIndex }),
}

export const auraApi = {
  getDevices: () => api.get<Array<Record<string, unknown>>>('/api/aura/devices'),
  setEffect: (deviceId: string, effect: string, params?: Record<string, unknown>) =>
    api.post<void>(`/api/aura/devices/${deviceId}/effect`, { effect, params }),
  setColor: (deviceId: string, color: string) =>
    api.post<void>(`/api/aura/devices/${deviceId}/color`, { color }),
  setBrightness: (deviceId: string, brightness: number) =>
    api.post<void>(`/api/aura/devices/${deviceId}/brightness`, { brightness }),
  setSpeed: (deviceId: string, speed: number) =>
    api.post<void>(`/api/aura/devices/${deviceId}/speed`, { speed }),
  setIntensity: (deviceId: string, intensity: number) =>
    api.post<void>(`/api/aura/devices/${deviceId}/intensity`, { intensity }),
  setEnable: (deviceId: string, enabled: boolean) =>
    api.post<void>(`/api/aura/devices/${deviceId}/enable`, { enabled }),
  getPresets: () => api.get<Array<Record<string, unknown>>>('/api/aura/presets'),
  savePreset: (name: string) =>
    api.post<void>('/api/aura/presets', { name }),
}