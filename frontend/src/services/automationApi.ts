import { api } from './api'

export interface AutomationRule {
  trigger: string
  performanceMode?: string
  gpuMode?: string
  refreshRate?: number
  keyboardTimeoutSeconds?: number
  chargeLimit?: number
  optimizeGpu: boolean
  name?: string
}

export interface AutomationStatus {
  isRunning: boolean
  isEnabled: boolean
  rules: AutomationRule[]
}

export const automationApi = {
  getStatus: () => api.get<AutomationStatus>('/api/automation/status'),
  start: () => api.post<boolean>('/api/automation/start'),
  stop: () => api.post<boolean>('/api/automation/stop'),
  getRules: () => api.get<AutomationRule[]>('/api/automation/rules'),
  addRule: (rule: Omit<AutomationRule, 'name'> & { name?: string }) => api.post<boolean>('/api/automation/rules', rule),
  removeRule: (name: string) => api.delete<boolean>(`/api/automation/rules/${name}`),
  updateConfig: (isEnabled: boolean) => api.put<boolean>('/api/automation/config', { isEnabled }),
  applyNow: (trigger: string) => api.post<boolean>('/api/automation/apply', { trigger }),
}