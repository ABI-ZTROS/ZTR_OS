import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ModeCard } from '@/components/common/ModeCard'
import { SliderControl } from '@/components/common/SliderControl'
import { Gauge } from '@/components/common/Gauge'
import { FanCurveEditor } from '@/components/common/FanCurveEditor'
import { useHardwareStore } from '@/store/useHardwareStore'
import { performanceApi } from '@/services/performanceApi'
import type { PerformanceMode, FanCurvePoint } from '@/types'
import './Performance.css'

const MODES: { id: PerformanceMode; label: string; desc: string; icon: string; color: 'primary' | 'secondary' | 'accent' }[] = [
  { id: 'silent', label: 'Silent', desc: 'Minimal fan noise', icon: '🤫', color: 'accent' },
  { id: 'balanced', label: 'Balanced', desc: 'Default tuning', icon: '⚖', color: 'primary' },
  { id: 'turbo', label: 'Turbo', desc: 'Max performance', icon: '🚀', color: 'secondary' },
  { id: 'fullspeed', label: 'Full Speed', desc: 'Fans at max', icon: '💨', color: 'secondary' },
  { id: 'manual', label: 'Manual', desc: 'Custom curves', icon: '🎛', color: 'accent' },
]

export function Performance() {
  const hardware = useHardwareStore((s) => s.hardware)
  const isConnected = useHardwareStore((s) => s.isConnected)

  const [mode, setMode] = useState<PerformanceMode>('balanced')
  const [cpuPower, setCpuPower] = useState(0)
  const [gpuPower, setGpuPower] = useState(0)
  const [spl, setSpl] = useState(0)
  const [sppt, setSppt] = useState(0)
  const [fppt, setFppt] = useState(0)
  const [cpuCurve, setCpuCurve] = useState<FanCurvePoint[]>([])
  const [gpuCurve, setGpuCurve] = useState<FanCurvePoint[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadConfig = useCallback(async () => {
    try {
      setIsLoading(true)
      const [configRes, powerRes, curvesRes] = await Promise.allSettled([
        performanceApi.getConfig(),
        performanceApi.getPowerLimit(),
        performanceApi.getFanCurves(),
      ])

      if (configRes.status === 'fulfilled' && configRes.value.success) {
        const config = configRes.value.data
        if (config.mode) setMode(config.mode as unknown as PerformanceMode)
      }

      if (powerRes.status === 'fulfilled' && powerRes.value.success) {
        const power = powerRes.value.data
        setCpuPower(power.cpu ?? 0)
        setGpuPower(power.gpu ?? 0)
        setSpl((power as Record<string, number>).spl ?? 0)
        setSppt((power as Record<string, number>).sppt ?? 0)
        setFppt((power as Record<string, number>).fppt ?? 0)
      }

      if (curvesRes.status === 'fulfilled' && curvesRes.value.success) {
        const curves = curvesRes.value.data
        if (curves.cpu) setCpuCurve((curves.cpu as number[]).map((speed, i) => ({ temperature: i * 20, speed })))
        if (curves.gpu) setGpuCurve((curves.gpu as number[]).map((speed, i) => ({ temperature: i * 20, speed })))
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load config')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadConfig()
  }, [loadConfig])

  const handleModeChange = useCallback(async (newMode: PerformanceMode) => {
    setMode(newMode)
    try {
      await performanceApi.setGpuMode(newMode)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set mode')
    }
  }, [])

  const handleCpuPower = useCallback(async (value: number) => {
    setCpuPower(value)
    try {
      await performanceApi.setPowerLimit('cpu', value)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set CPU power limit')
    }
  }, [])

  const handleGpuPower = useCallback(async (value: number) => {
    setGpuPower(value)
    try {
      await performanceApi.setPowerLimit('gpu', value)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set GPU power limit')
    }
  }, [])

  const handleCpuCurveChange = useCallback(async (points: FanCurvePoint[]) => {
    setCpuCurve(points)
    try {
      await performanceApi.setFanCurve(0, points.map((p) => p.speed))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save CPU fan curve')
    }
  }, [])

  const handleGpuCurveChange = useCallback(async (points: FanCurvePoint[]) => {
    setGpuCurve(points)
    try {
      await performanceApi.setFanCurve(1, points.map((p) => p.speed))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save GPU fan curve')
    }
  }, [])

  const cpu = hardware?.cpu
  const gpu = hardware?.gpu

  return (
    <PageWrapper
      title="Performance Control"
      subtitle="CPU/GPU power limits, fan curves, and GPU performance modes"
      actions={
        <button className="btn-ghost" onClick={loadConfig} disabled={isLoading}>
          {isLoading ? 'Loading...' : 'Refresh'}
        </button>
      }
    >
      {error && (
        <div className="performance-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <GlowCard title="Performance Mode" glowColor="primary">
        <div className="mode-selector">
          {MODES.map((m) => (
            <ModeCard
              key={m.id}
              title={m.label}
              description={m.desc}
              icon={<span>{m.icon}</span>}
              selected={mode === m.id}
              onClick={() => handleModeChange(m.id)}
              glowColor={m.color}
            />
          ))}
        </div>
      </GlowCard>

      <div className="grid-2">
        <GlowCard title="CPU Telemetry" glowColor="accent">
          <div className="gauge-row">
            <Gauge
              config={{
                label: 'CPU Temp',
                value: cpu?.temperature ?? 0,
                max: 100,
                unit: '°C',
                color: '#00aaff',
              }}
            />
            <Gauge
              config={{
                label: 'CPU Power',
                value: cpu?.powerDraw ?? 0,
                max: 250,
                unit: 'W',
                color: '#00ffaa',
              }}
            />
            <Gauge
              config={{
                label: 'CPU Usage',
                value: cpu?.usage ?? 0,
                max: 100,
                unit: '%',
                color: '#ffaa00',
              }}
            />
          </div>
        </GlowCard>

        <GlowCard title="GPU Telemetry" glowColor="secondary">
          <div className="gauge-row">
            <Gauge
              config={{
                label: 'GPU Temp',
                value: gpu?.temperature ?? 0,
                max: 100,
                unit: '°C',
                color: '#ff00aa',
              }}
            />
            <Gauge
              config={{
                label: 'GPU Power',
                value: gpu?.powerDraw ?? 0,
                max: 450,
                unit: 'W',
                color: '#00ffaa',
              }}
            />
            <Gauge
              config={{
                label: 'GPU Usage',
                value: gpu?.usage ?? 0,
                max: 100,
                unit: '%',
                color: '#00aaff',
              }}
            />
          </div>
        </GlowCard>
      </div>

      <GlowCard title="Power Limits" glowColor="accent">
        <div className="power-limits-grid">
          <SliderControl
            label="CPU Power Limit"
            value={cpuPower}
            min={10}
            max={300}
            step={5}
            unit="W"
            onChange={handleCpuPower}
            color="accent"
          />
          <SliderControl
            label="GPU Power Limit"
            value={gpuPower}
            min={50}
            max={500}
            step={10}
            unit="W"
            onChange={handleGpuPower}
            color="primary"
          />
        </div>
        <div className="divider" />
        <div className="power-limits-grid">
          <SliderControl
            label="SPL (Smart Power Limit)"
            value={spl}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={(v) => setSpl(v)}
            color="primary"
          />
          <SliderControl
            label="sPPT (Smart Power Push)"
            value={sppt}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={(v) => setSppt(v)}
            color="accent"
          />
          <SliderControl
            label="fPPT (Fan Power Push)"
            value={fppt}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={(v) => setFppt(v)}
            color="secondary"
          />
        </div>
      </GlowCard>

      {mode === 'manual' && (
        <div className="grid-2">
          <GlowCard title="CPU Fan Curve" glowColor="accent">
            {cpuCurve.length >= 2 ? (
              <FanCurveEditor
                points={cpuCurve}
                onChange={handleCpuCurveChange}
                label="CPU Fan Response"
              />
            ) : (
              <p className="placeholder-text">No CPU fan curve data. Connect to backend to load curves.</p>
            )}
          </GlowCard>

          <GlowCard title="GPU Fan Curve" glowColor="primary">
            {gpuCurve.length >= 2 ? (
              <FanCurveEditor
                points={gpuCurve}
                onChange={handleGpuCurveChange}
                label="GPU Fan Response"
              />
            ) : (
              <p className="placeholder-text">No GPU fan curve data. Connect to backend to load curves.</p>
            )}
          </GlowCard>
        </div>
      )}

      <GlowCard title="Connected Fans" glowColor="primary">
        {hardware?.fans && hardware.fans.length > 0 ? (
          <div className="fans-list">
            {hardware.fans.map((fan) => (
              <div key={fan.id} className="fan-item">
                <div className="fan-info">
                  <span className="fan-name">{fan.name}</span>
                  <span className="fan-mode">
                    <span className={`chip ${fan.mode === 'manual' ? 'chip--active' : 'chip--info'}`}>
                      {fan.mode}
                    </span>
                  </span>
                </div>
                <div className="fan-speed-row">
                  <span className="fan-speed-label">Current</span>
                  <div className="progress-bar">
                    <div
                      className={`progress-bar-fill ${fan.speed > 80 ? 'progress-bar-fill--danger' : fan.speed > 50 ? 'progress-bar-fill--warning' : 'progress-bar-fill--primary'}`}
                      style={{ width: `${fan.speed}%` }}
                    />
                  </div>
                  <span className="fan-speed-value">{fan.speed.toFixed(0)}%</span>
                </div>
                <div className="fan-target-row">
                  <span className="fan-speed-label">Target</span>
                  <div className="progress-bar">
                    <div
                      className="progress-bar-fill progress-bar-fill--accent"
                      style={{ width: `${fan.targetSpeed}%` }}
                    />
                  </div>
                  <span className="fan-speed-value">{fan.targetSpeed.toFixed(0)}%</span>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="placeholder-text">
            {isConnected ? 'No fans detected.' : 'Waiting for backend connection...'}
          </p>
        )}
      </GlowCard>
    </PageWrapper>
  )
}
