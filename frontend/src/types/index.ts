export interface HardwareState {
  cpu: CpuState
  gpu: GpuState
  battery: BatteryState
  fans: FanState[]
  memory: MemoryState
}

export interface CpuState {
  usage: number
  temperature: number
  powerDraw: number
  coreCount: number
  threadCount: number
  cores: CoreState[]
}

export interface CoreState {
  id: number
  usage: number
  temperature: number
}

export interface GpuState {
  usage: number
  temperature: number
  powerDraw: number
  clockSpeed: number
  memoryUsed: number
  memoryTotal: number
  fans: number
}

export interface BatteryState {
  percentage: number
  status: string
  timeRemaining: number
  powerDraw: number
}

export interface FanState {
  id: number
  name: string
  speed: number
  targetSpeed: number
  mode: string
}

export interface MemoryState {
  used: number
  total: number
  available: number
}

export interface MlpConfig {
  learningRate: number
  hiddenLayers: number[]
  inputSize: number
  outputSize: number
  isTraining: boolean
  epochs: number
  currentEpoch: number
  loss: number
}

export interface MlpDecision {
  id: string
  timestamp: string
  input: number[]
  output: number[]
  confidence: number
  action: string
}

export interface MlpState {
  config: MlpConfig
  decisions: MlpDecision[]
  status: 'idle' | 'training' | 'running' | 'error'
  lastUpdated: string
}

export interface UserSettings {
  autoPerformance: boolean
  autoMlp: boolean
  autoAura: boolean
  pollingInterval: number
  theme: 'dark' | 'cyber'
  notificationsEnabled: boolean
  autoStart: boolean
  minimizeToTray: boolean
  predictionWindow: number
  autoModeSwitch: boolean
  hotkeys: HotkeyBinding[]
}

export interface HotkeyBinding {
  id: string
  action: string
  keys: string[]
}

export interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected' | 'reconnecting' | 'error'

export type PerformanceMode = 'silent' | 'balanced' | 'turbo' | 'fullspeed' | 'manual'

export interface FanCurvePoint {
  temperature: number
  speed: number
}

export interface FanCurve {
  id: number
  name: string
  points: FanCurvePoint[]
  isCustom: boolean
}

export interface PowerLimitConfig {
  cpu: number
  gpu: number
  spl: number
  sppt: number
  fppt: number
}

export interface ProcessInfo {
  id: number
  name: string
  cpuUsage: number
  memoryUsage: number
  affinity: number[]
  gpuAffinity?: number[]
  isGame?: boolean
}

export interface CpuTopologyNode {
  id: number
  type: 'package' | 'core' | 'thread'
  name: string
  children?: CpuTopologyNode[]
  usage?: number
  temperature?: number
}

export interface AuraDevice {
  id: string
  name: string
  type: 'keyboard' | 'body' | 'touchpad' | 'mouse' | 'headset'
  zone: string
  effects: AuraEffectType[]
  currentEffect?: AuraEffectType
  currentColor?: string
  brightness?: number
}

export type AuraEffectType =
  | 'static'
  | 'breathe'
  | 'rainbow'
  | 'audio'
  | 'heatmap'
  | 'wave'
  | 'ripple'
  | 'starry'

export interface AuraPreset {
  id: string
  name: string
  effects: AuraEffectConfig[]
}

export interface AuraEffectConfig {
  deviceId: string
  effect: AuraEffectType
  color: string
  brightness: number
  speed: number
  intensity: number
}

export interface BindingConfig {
  processId: number
  processName: string
  cpuCores: number[]
  gpuDevices: number[]
}

export interface TimelineEvent {
  id: string
  timestamp: string
  type: 'decision' | 'config' | 'training' | 'system'
  title: string
  description?: string
  metadata?: Record<string, unknown>
}

export interface GaugeConfig {
  label: string
  value: number
  max: number
  unit: string
  color: string
}
