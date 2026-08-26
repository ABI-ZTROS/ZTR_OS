import { create } from 'zustand'
import type { ConnectionStatus, HardwareState } from '@/types'
import { signalRService } from '@/services/signalR'
import { hardwareApi } from '@/services/hardwareApi'

interface HardwareStore {
  connectionStatus: ConnectionStatus
  hardware: HardwareState | null
  isConnected: boolean
  lastUpdated: Date | null
  error: string | null
  subscribe: () => void
  unsubscribe: () => void
  fetchOnce: () => Promise<void>
  startPolling: () => void
  stopPolling: () => void
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
let pollingTimer: ReturnType<typeof setInterval> | null = null

export const useHardwareStore = create<HardwareStore>((set, get) => ({
  connectionStatus: 'disconnected',
  hardware: defaultHardware,
  isConnected: false,
  lastUpdated: null,
  error: null,

  fetchOnce: async () => {
    try {
      const res = await hardwareApi.getState()
      if (res.success && res.data) {
        set({
          hardware: res.data,
          isConnected: true,
          lastUpdated: new Date(),
          error: null,
        })
      }
    } catch (e) {
      set({
        isConnected: false,
        error: e instanceof Error ? e.message : 'Failed to fetch hardware data',
      })
    }
  },

  startPolling: () => {
    if (pollingTimer) return

    get().fetchOnce()

    pollingTimer = setInterval(async () => {
      await get().fetchOnce()
    }, 1000)
  },

  stopPolling: () => {
    if (pollingTimer) {
      clearInterval(pollingTimer)
      pollingTimer = null
    }
  },

  subscribe: () => {
    if (subscribed) return
    subscribed = true

    signalRService.on<ConnectionStatus>('connection:status', (status) => {
      set({ connectionStatus: status })
    })

    signalRService.on<HardwareState>('hardware:update', (data) => {
      set({
        hardware: data,
        isConnected: true,
        lastUpdated: new Date(),
        error: null,
      })
    })
  },

  unsubscribe: () => {
    subscribed = false
    if (pollingTimer) {
      clearInterval(pollingTimer)
      pollingTimer = null
    }
  },
}))
