import { api } from './api'

export interface GpuTuningState {
  coreClockOffset: number
  memoryClockOffset: number
  powerLimit: number
  temperatureLimit: number
  dynamicBoostLevel: number
  voltageOffset: number
}

export interface GpuLiveState {
  temperature: number
  hotspotTemperature: number
  usage: number
  power: number
  coreClockMHz: number
  memoryClockMHz: number
  usedVramMB: number
  totalVramMB: number
}

export interface GpuStateResponse {
  tuning: GpuTuningState
  live: GpuLiveState
}

export interface SetClocksRequest {
  coreOffset: number
  memoryOffset: number
}

export interface SetGpuPowerRequest {
  watts: number
}

export interface SetTempLimitRequest {
  temperature: number
}

export interface SetDynamicBoostRequest {
  level: number
}

export interface SetVoltageRequest {
  offset: number
}

export interface SetGpuModeRequest {
  mode: string
}

export const gpuApi = {
  getState: () => api.get<GpuStateResponse>('/api/gpu/state'),
  getClocks: () => api.get<{ coreClockOffset: number; memoryClockOffset: number }>('/api/gpu/clocks'),
  setClocks: (data: SetClocksRequest) => api.post<boolean>('/api/gpu/clocks', data),
  setPower: (watts: number) => api.post<boolean>('/api/gpu/power', { watts }),
  setTempLimit: (temperature: number) => api.post<boolean>('/api/gpu/temp-limit', { temperature }),
  setDynamicBoost: (level: number) => api.post<boolean>('/api/gpu/dynamic-boost', { level }),
  setVoltage: (offset: number) => api.post<boolean>('/api/gpu/voltage', { offset }),
  reset: () => api.post<boolean>('/api/gpu/reset'),
  getMode: () => api.get<{ mode: string }>('/api/gpu/mode'),
  setMode: (mode: string) => api.post<boolean>('/api/gpu/mode', { mode }),
  setOptimized: () => api.post<boolean>('/api/gpu/optimized'),
}