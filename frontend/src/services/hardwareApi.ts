import { api } from './api'
import type { HardwareState } from '@/types'

export const hardwareApi = {
  getState: () => api.get<HardwareState>('/api/hardware/state'),
  getCpu: () => api.get<HardwareState['cpu']>('/api/hardware/cpu'),
  getGpu: () => api.get<HardwareState['gpu']>('/api/hardware/gpu'),
  getBattery: () => api.get<HardwareState['battery']>('/api/hardware/battery'),
  getFan: () => api.get<HardwareState['fans']>('/api/hardware/fan'),
}