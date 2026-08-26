import { create } from 'zustand'
import type { MlpConfig, MlpDecision, MlpState } from '@/types'
import { signalRService } from '@/services/signalR'

interface MlpStore {
  state: MlpState
  decisions: MlpDecision[]
  subscribe: () => void
  unsubscribe: () => void
  addDecision: (decision: MlpDecision) => void
  updateState: (state: Partial<MlpState>) => void
}

const defaultConfig: MlpConfig = {
  learningRate: 0.001,
  hiddenLayers: [64, 32, 16],
  inputSize: 10,
  outputSize: 4,
  isTraining: false,
  epochs: 0,
  currentEpoch: 0,
  loss: 0,
}

const defaultState: MlpState = {
  config: defaultConfig,
  decisions: [],
  status: 'idle',
  lastUpdated: new Date().toISOString(),
}

let subscribed = false

export const useMlpStore = create<MlpStore>((set) => ({
  state: defaultState,
  decisions: [],

  subscribe: () => {
    if (subscribed) return
    subscribed = true

    signalRService.on<MlpState>('mlp:state', (data) => {
      set({ state: data })
    })

    signalRService.on<MlpDecision>('mlp:decision', (decision) => {
      set((store) => ({
        decisions: [decision, ...store.decisions].slice(0, 100),
      }))
    })
  },

  unsubscribe: () => {
    subscribed = false
  },

  addDecision: (decision) => {
    set((store) => ({
      decisions: [decision, ...store.decisions].slice(0, 100),
    }))
  },

  updateState: (partial) => {
    set((store) => ({
      state: { ...store.state, ...partial },
    }))
  },
}))