import { api } from './api'
import type { HardwareState } from '@/types'

export const hardwareApi = {
  getState: () => api.get<HardwareState>('/api/hardware/state'),
  getCpu: () => api.get<HardwareState['cpu']>('/api/hardware/cpu'),
  getGpu: () => api.get<HardwareState['gpu']>('/api/hardware/gpu'),
  getBattery: () => api.get<HardwareState['battery']>('/api/hardware/battery'),
  getFans: () => api.get<HardwareState['fans']>('/api/hardware/fans'),
  getMemory: () => api.get<HardwareState['memory']>('/api/hardware/memory'),
  setFanSpeed: (fanId: number, speed: number) =>
    api.post<void>(`/api/hardware/fans/${fanId}/speed`, { speed }),
  setFanMode: (fanId: number, mode: string) =>
    api.post<void>(`/api/hardware/fans/${fanId}/mode`, { mode }),
}