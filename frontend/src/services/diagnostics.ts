import { api } from './api'
import type { UserSettings } from '@/types'

export interface DiagnosticsReport {
  timestamp: string
  services: {
    aspiAvailable: boolean
    acpiAvailable: boolean
    hidInitialized: boolean
    hidDeviceCount: number
    modeControlActive: boolean
    lastCollectionSucceeded: boolean
    lastCollectionError?: string | null
  }
  lastHardwareState?: {
    cpu?: { temperature: number; usage: number; power: number }
    gpu?: { temperature: number; usage: number }
    battery?: { chargePercent: number }
    fan?: { cpuSpeed: number; gpuSpeed: number }
  } | null
}

export interface ApiResponse<T> {
  success: boolean
  data?: T | null
  error?: string | null
}

export const diagnosticsService = {
  async getDiagnostics(): Promise<ApiResponse<DiagnosticsReport>> {
    return api.get<DiagnosticsReport>('/api/diagnostics')
  },

  async checkBackendHealth(): Promise<{ healthy: boolean; message: string }> {
    try {
      const response = await fetch('/health')
      if (response.ok) {
        return { healthy: true, message: 'Backend API is healthy' }
      }
      return { healthy: false, message: `Backend returned ${response.status}` }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error'
      return { healthy: false, message: `Cannot reach backend: ${message}` }
    }
  },
}

export const settingsApi = {
  getUserSettings: () =>
    api.get<UserSettings>('/api/settings/user'),
  updateUserSettings: (settings: Partial<UserSettings>) =>
    api.put<void>('/api/settings/user', { settings }),
  resetUserSettings: () =>
    api.put<void>('/api/settings/user', {
      settings: {
        autoPerformance: true,
        autoMlp: true,
        autoAura: true,
        pollingInterval: 2000,
        theme: 'cyber',
        notificationsEnabled: true,
        autoStart: false,
        minimizeToTray: true,
        predictionWindow: 50,
        autoModeSwitch: true,
      }
    }),
}
