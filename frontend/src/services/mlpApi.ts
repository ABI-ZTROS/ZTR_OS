import { api } from './api'
import type { MlpConfig } from '@/types'

export const mlpApi = {
  getConfig: () => api.get<MlpConfig>('/api/mlp/config'),
  setConfig: (config: MlpConfig) =>
    api.put<void>('/api/mlp/config', config),
  startTraining: (config: MlpConfig) =>
    api.post<void>('/api/mlp/train', config),
  stopTraining: () => api.post<void>('/api/mlp/stop', {}),
  getStatus: () => api.get<{ status: string; loss: number; epoch: number }>('/api/mlp/status'),
  getDecisions: () => api.get<{ decisions: Array<Record<string, unknown>> }>('/api/mlp/decisions'),
  resetModel: () => api.post<void>('/api/mlp/reset', {}),
}