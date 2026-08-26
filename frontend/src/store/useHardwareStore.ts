import { create } from 'zustand'
import type { ConnectionStatus, HardwareState } from '@/types'
import { signalRService } from '@/services/signalR'

interface HardwareStore {
  connectionStatus: ConnectionStatus
  hardware: HardwareState | null
  isConnected: boolean
  subscribe: () => void
  unsubscribe: () => void
}

const defaultHardware: HardwareState = {
  cpu: {
    usage: 0,
    temperature: 0,
    powerDraw: 0,
    coreCount: 0,
    threadCount: 0,
    cores: [],
  },
  gpu: {
    usage: 0,
    temperature: 0,
    powerDraw: 0,
    clockSpeed: 0,
    memoryUsed: 0,
    memoryTotal: 0,
    fans: 0,
  },
  battery: {
    percentage: 100,
    status: 'AC',
    timeRemaining: 0,
    powerDraw: 0,
  },
  fans: [],
  memory: {
    used: 0,
    total: 0,
    available: 0,
  },
}

let subscribed = false

export const useHardwareStore = create<HardwareStore>((set) => ({
  connectionStatus: 'disconnected',
  hardware: defaultHardware,
  isConnected: false,

  subscribe: () => {
    if (subscribed) return
    subscribed = true

    signalRService.on<ConnectionStatus>('connection:status', (status) => {
      set({ connectionStatus: status, isConnected: status === 'connected' })
    })

    signalRService.on<HardwareState>('hardware:update', (data) => {
      set({ hardware: data })
    })
  },

  unsubscribe: () => {
    subscribed = false
  },
}))