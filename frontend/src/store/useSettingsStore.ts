import { create } from 'zustand'
import type { UserSettings } from '@/types'

interface SettingsStore {
  settings: UserSettings
  updateSettings: (partial: Partial<UserSettings>) => void
  resetSettings: () => void
}

const defaultSettings: UserSettings = {
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
  hotkeys: [
    { id: '1', action: 'Toggle Performance', keys: ['Ctrl', 'Shift', 'P'] },
    { id: '2', action: 'Toggle Silent Mode', keys: ['Ctrl', 'Shift', 'S'] },
    { id: '3', action: 'Boost CPU', keys: ['Ctrl', 'Alt', 'C'] },
    { id: '4', action: 'Toggle Aura', keys: ['Ctrl', 'Shift', 'A'] },
    { id: '5', action: 'Open Dashboard', keys: ['Ctrl', 'Shift', 'D'] },
  ],
}

const STORAGE_KEY = 'ztr_settings'

function loadSettings(): UserSettings {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      return { ...defaultSettings, ...JSON.parse(saved) }
    }
  } catch {
    // ignore
  }
  return defaultSettings
}

function saveSettings(settings: UserSettings): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings))
  } catch {
    // ignore
  }
}

export const useSettingsStore = create<SettingsStore>((set) => ({
  settings: loadSettings(),

  updateSettings: (partial) => {
    set((store) => {
      const updated = { ...store.settings, ...partial }
      saveSettings(updated)
      return { settings: updated }
    })
  },

  resetSettings: () => {
    saveSettings(defaultSettings)
    set({ settings: defaultSettings })
  },
}))
