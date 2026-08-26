export interface OverlayConfig {
  enabled: boolean
  showFps: boolean
  showCpuTemp: boolean
  showGpuTemp: boolean
  showFanSpeed: boolean
  showPowerDraw: boolean
  showBattery: boolean
  position: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right'
  opacity: number
  fontSize: number
  updateIntervalMs: number
}

export interface OverlayMetrics {
  fps: number
  cpuTemp: number
  gpuTemp: number
  fanSpeed: number
  powerDraw: number
  batteryLevel: number
  timestamp: number
}

const DEFAULT_CONFIG: OverlayConfig = {
  enabled: false,
  showFps: true,
  showCpuTemp: true,
  showGpuTemp: true,
  showFanSpeed: false,
  showPowerDraw: true,
  showBattery: true,
  position: 'top-right',
  opacity: 0.85,
  fontSize: 12,
  updateIntervalMs: 500,
}

export class OverlayService {
  private config: OverlayConfig
  private metrics: OverlayMetrics
  private listeners: Set<(metrics: OverlayMetrics) => void>
  private animationFrameId: number | null
  private lastFrameTime: number
  private frameCount: number

  constructor() {
    this.config = this.loadConfig()
    this.metrics = {
      fps: 0, cpuTemp: 0, gpuTemp: 0,
      fanSpeed: 0, powerDraw: 0, batteryLevel: 100,
      timestamp: Date.now(),
    }
    this.listeners = new Set()
    this.animationFrameId = null
    this.lastFrameTime = performance.now()
    this.frameCount = 0
  }

  private loadConfig(): OverlayConfig {
    try {
      const saved = localStorage.getItem('ztr_overlay_config')
      if (saved) return { ...DEFAULT_CONFIG, ...JSON.parse(saved) }
    } catch {}
    return { ...DEFAULT_CONFIG }
  }

  private saveConfig() {
    localStorage.setItem('ztr_overlay_config', JSON.stringify(this.config))
  }

  getConfig(): OverlayConfig {
    return { ...this.config }
  }

  updateConfig(partial: Partial<OverlayConfig>) {
    this.config = { ...this.config, ...partial }
    this.saveConfig()
    if (this.config.enabled) this.start()
    else this.stop()
  }

  subscribe(listener: (metrics: OverlayMetrics) => void): () => void {
    this.listeners.add(listener)
    return () => this.listeners.delete(listener)
  }

  start() {
    if (this.animationFrameId !== null) return
    this.lastFrameTime = performance.now()
    this.frameCount = 0
    const tick = () => {
      const now = performance.now()
      this.frameCount++
      if (now - this.lastFrameTime >= 1000) {
        this.metrics.fps = this.frameCount
        this.frameCount = 0
        this.lastFrameTime = now
        this.emitMetrics()
      }
      this.animationFrameId = requestAnimationFrame(tick)
    }
    this.animationFrameId = requestAnimationFrame(tick)
  }

  stop() {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId)
      this.animationFrameId = null
    }
  }

  private emitMetrics() {
    this.metrics = { ...this.metrics, timestamp: Date.now() }
    this.listeners.forEach(listener => listener({ ...this.metrics }))
  }

  updateMetric<K extends keyof OverlayMetrics>(key: K, value: OverlayMetrics[K]) {
    this.metrics = { ...this.metrics, [key]: value }
    this.emitMetrics()
  }

  reset() {
    this.metrics = {
      fps: 0, cpuTemp: 0, gpuTemp: 0,
      fanSpeed: 0, powerDraw: 0, batteryLevel: 100,
      timestamp: Date.now(),
    }
    this.emitMetrics()
  }
}

export const overlayService = new OverlayService()