import { useState, useEffect, useCallback } from 'react'
import { overlayService, type OverlayConfig, type OverlayMetrics } from '@/services/overlayService'
import { useHardwareStore } from '@/store/useHardwareStore'
import './GameOverlay.css'

export function GameOverlay() {
  const [config, setConfig] = useState<OverlayConfig>(() => overlayService.getConfig())
  const [metrics, setMetrics] = useState<OverlayMetrics>(() => ({
    fps: 0,
    cpuTemp: 0,
    gpuTemp: 0,
    fanSpeed: 0,
    powerDraw: 0,
    batteryLevel: 100,
    timestamp: 0,
  }))

  const hardware = useHardwareStore((s) => s.hardware)

  useEffect(() => {
    const unsubscribe = overlayService.subscribe((newMetrics) => {
      setMetrics(newMetrics)
    })

    overlayService.start()

    return () => {
      unsubscribe()
    }
  }, [])

  useEffect(() => {
    if (!config.enabled) return

    if (hardware) {
      overlayService.updateMetric('cpuTemp', hardware.cpu?.temperature ?? 0)
      overlayService.updateMetric('gpuTemp', hardware.gpu?.temperature ?? 0)
      overlayService.updateMetric('powerDraw', hardware.gpu?.powerDraw ?? 0)
      overlayService.updateMetric('batteryLevel', hardware.battery?.percentage ?? 100)

      const fans = hardware.fans ?? []
      const gpuFan = fans.find((f) => f.name.toLowerCase().includes('gpu'))
      if (gpuFan) {
        overlayService.updateMetric('fanSpeed', gpuFan.speed)
      }
    }
  }, [hardware, config.enabled])

  const handleToggle = useCallback(() => {
    const newConfig = { ...config, enabled: !config.enabled }
    setConfig(newConfig)
    overlayService.updateConfig(newConfig)
  }, [config])

  const handlePositionChange = useCallback((position: OverlayConfig['position']) => {
    const newConfig = { ...config, position }
    setConfig(newConfig)
    overlayService.updateConfig(newConfig)
  }, [config])

  const handleToggleMetric = useCallback((key: keyof OverlayConfig) => {
    const newConfig = { ...config, [key]: !config[key] } as OverlayConfig
    setConfig(newConfig)
    overlayService.updateConfig(newConfig)
  }, [config])

  if (!config.enabled) {
    return (
      <div className="game-overlay game-overlay--disabled">
        <div className="game-overlay-toggle" onClick={handleToggle}>
          <span className="game-overlay-toggle-icon">◈</span>
          <span>开启游戏覆盖层</span>
        </div>
      </div>
    )
  }

  const positionClasses: Record<OverlayConfig['position'], string> = {
    'top-left': 'game-overlay--top-left',
    'top-right': 'game-overlay--top-right',
    'bottom-left': 'game-overlay--bottom-left',
    'bottom-right': 'game-overlay--bottom-right',
  }

  return (
    <>
      <div
        className={`game-overlay game-overlay--active ${positionClasses[config.position]}`}
        style={{ opacity: config.opacity, fontSize: `${config.fontSize}px` }}
      >
        <div className="game-overlay-header">
          <span className="game-overlay-title">ZTR Monitor</span>
          <span className="game-overlay-fps">{metrics.fps} FPS</span>
        </div>

        <div className="game-overlay-body">
          {config.showCpuTemp && (
            <div className="game-overlay-row">
              <span className="game-overlay-label">CPU</span>
              <span className="game-overlay-value">{metrics.cpuTemp.toFixed(0)}°C</span>
            </div>
          )}

          {config.showGpuTemp && (
            <div className="game-overlay-row">
              <span className="game-overlay-label">GPU</span>
              <span className="game-overlay-value">{metrics.gpuTemp.toFixed(0)}°C</span>
            </div>
          )}

          {config.showFanSpeed && (
            <div className="game-overlay-row">
              <span className="game-overlay-label">Fan</span>
              <span className="game-overlay-value">{metrics.fanSpeed.toFixed(0)}%</span>
            </div>
          )}

          {config.showPowerDraw && (
            <div className="game-overlay-row">
              <span className="game-overlay-label">Power</span>
              <span className="game-overlay-value">{metrics.powerDraw.toFixed(1)}W</span>
            </div>
          )}

          {config.showBattery && (
            <div className="game-overlay-row">
              <span className="game-overlay-label">Bat</span>
              <span className={`game-overlay-value ${metrics.batteryLevel < 20 ? 'game-overlay-value--danger' : ''}`}>
                {metrics.batteryLevel.toFixed(0)}%
              </span>
            </div>
          )}
        </div>
      </div>

      <div className="game-overlay-settings">
        <div className="game-overlay-settings-header">
          <span>覆盖层设置</span>
          <button className="game-overlay-close" onClick={handleToggle}>×</button>
        </div>

        <div className="game-overlay-settings-row">
          <span>位置</span>
          <div className="game-overlay-position-grid">
            {(['top-left', 'top-right', 'bottom-left', 'bottom-right'] as const).map((pos) => (
              <button
                key={pos}
                className={`game-overlay-position-btn ${config.position === pos ? 'game-overlay-position-btn--active' : ''}`}
                onClick={() => handlePositionChange(pos)}
                title={pos}
              >
                {pos === 'top-left' ? '↖' : pos === 'top-right' ? '↗' : pos === 'bottom-left' ? '↙' : '↘'}
              </button>
            ))}
          </div>
        </div>

        <div className="game-overlay-settings-row">
          <label className="game-overlay-checkbox">
            <input
              type="checkbox"
              checked={config.showFps}
              onChange={() => handleToggleMetric('showFps')}
            />
            <span>FPS</span>
          </label>
          <label className="game-overlay-checkbox">
            <input
              type="checkbox"
              checked={config.showCpuTemp}
              onChange={() => handleToggleMetric('showCpuTemp')}
            />
            <span>CPU温度</span>
          </label>
          <label className="game-overlay-checkbox">
            <input
              type="checkbox"
              checked={config.showGpuTemp}
              onChange={() => handleToggleMetric('showGpuTemp')}
            />
            <span>GPU温度</span>
          </label>
          <label className="game-overlay-checkbox">
            <input
              type="checkbox"
              checked={config.showFanSpeed}
              onChange={() => handleToggleMetric('showFanSpeed')}
            />
            <span>风扇</span>
          </label>
          <label className="game-overlay-checkbox">
            <input
              type="checkbox"
              checked={config.showPowerDraw}
              onChange={() => handleToggleMetric('showPowerDraw')}
            />
            <span>功耗</span>
          </label>
          <label className="game-overlay-checkbox">
            <input
              type="checkbox"
              checked={config.showBattery}
              onChange={() => handleToggleMetric('showBattery')}
            />
            <span>电池</span>
          </label>
        </div>
      </div>
    </>
  )
}
