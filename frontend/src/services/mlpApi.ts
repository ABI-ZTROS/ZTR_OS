import { api } from './api'

export interface MlpConfigResponse {
  learningRate: number
  hiddenLayers: number[]
  inputSize: number
  outputSize: number
  isTraining: boolean
  epochs: number
  currentEpoch: number
  loss: number
}

export interface MlpDecisionResponse {
  id: string
  timestamp: string
  input: number[]
  output: number[]
  confidence: number
  action: string
}

export interface MlpStatusResponse {
  status: string
  loss: number
  epoch: number
}

export const mlpApi = {
  getConfig: () => api.get<MlpConfigResponse>('/api/mlp/config'),
  setConfig: (config: MlpConfigResponse) =>
    api.put<void>('/api/mlp/config', { config }),
  startTraining: (config?: MlpConfigResponse) =>
    api.post<void>('/api/mlp/train', { config }),
  stopTraining: () => api.post<void>('/api/mlp/stop', {}),
  getStatus: () => api.get<MlpStatusResponse>('/api/mlp/status'),
  getDecisions: () => api.get<MlpDecisionResponse[]>('/api/mlp/decisions'),
  resetModel: () => api.post<void>('/api/mlp/reset', {}),
}
