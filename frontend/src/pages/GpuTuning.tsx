import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ModeCard } from '@/components/common/ModeCard'
import { SliderControl } from '@/components/common/SliderControl'
import { Gauge } from '@/components/common/Gauge'
import { Reveal } from '@/components/common/Reveal'
import { useHardwareStore } from '@/store/useHardwareStore'
import { gpuApi, type GpuTuningState, type GpuLiveState } from '@/services/gpuApi'
import './GpuTuning.css'

const GPU_MODES: { id: string; label: string; desc: string; icon: string; color: 'primary' | 'secondary' | 'accent' }[] = [
  { id: 'eco', label: 'Eco', desc: '节能模式', icon: '🌱', color: 'accent' },
  { id: 'standard', label: 'Standard', desc: '标准模式', icon: '⚖', color: 'primary' },
  { id: 'ultimate', label: 'Ultimate', desc: '极致性能', icon: '🔥', color: 'secondary' },
  { id: 'optimized', label: 'Optimized', desc: '优化模式', icon: '✨', color: 'primary' },
]

const DYNAMIC_BOOST_OPTIONS = [
  { value: 0, label: 'Off' },
  { value: 5, label: '5W' },
  { value: 15, label: '15W' },
  { value: 20, label: '20W' },
]

export function GpuTuning() {
  const hardware = useHardwareStore((s) => s.hardware)

  const [tuning, setTuning] = useState<GpuTuningState>({
    coreClockOffset: 0,
    memoryClockOffset: 0,
    powerLimit: 150,
    temperatureLimit: 83,
    dynamicBoostLevel: 0,
    voltageOffset: 0,
  })

  const [live, setLive] = useState<GpuLiveState>({
    temperature: 0,
    hotspotTemperature: 0,
    usage: 0,
    power: 0,
    coreClockMHz: 0,
    memoryClockMHz: 0,
    usedVramMB: 0,
    totalVramMB: 0,
  })

  const [gpuMode, setGpuMode] = useState<string>('standard')
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isNvidia, setIsNvidia] = useState(false)
  const [isResetting, setIsResetting] = useState(false)

  const loadState = useCallback(async () => {
    try {
      setIsRefreshing(true)
      const res = await gpuApi.getState()
      if (res.success && res.data) {
        setTuning(res.data.tuning)
        setLive(res.data.live)
      }
      const modeRes = await gpuApi.getMode()
      if (modeRes.success && modeRes.data) {
        setGpuMode(modeRes.data.mode)
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : '加载GPU调优状态失败')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }, [])

  useEffect(() => {
    loadState()
  }, [loadState])

  useEffect(() => {
    const gpu = hardware?.gpu
    if (gpu) {
      setLive((prev) => ({
        ...prev,
        temperature: gpu.temperature,
        usage: gpu.usage,
        power: gpu.powerDraw,
        coreClockMHz: gpu.clockSpeed,
        usedVramMB: gpu.memoryUsed,
        totalVramMB: gpu.memoryTotal,
      }))
    }
    const name = gpu ? 'GPU' : ''
    if (name.toLowerCase().includes('nvidia')) {
      setIsNvidia(true)
    }
  }, [hardware])

  const handleCoreClock = useCallback(async (value: number) => {
    setTuning((prev) => ({ ...prev, coreClockOffset: value }))
    try {
      await gpuApi.setClocks({ coreOffset: value, memoryOffset: tuning.memoryClockOffset })
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置核心时钟失败')
    }
  }, [tuning.memoryClockOffset])

  const handleMemoryClock = useCallback(async (value: number) => {
    setTuning((prev) => ({ ...prev, memoryClockOffset: value }))
    try {
      await gpuApi.setClocks({ coreOffset: tuning.coreClockOffset, memoryOffset: value })
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置显存时钟失败')
    }
  }, [tuning.coreClockOffset])

  const handlePowerLimit = useCallback(async (value: number) => {
    setTuning((prev) => ({ ...prev, powerLimit: value }))
    try {
      await gpuApi.setPower(value)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置功耗限制失败')
    }
  }, [])

  const handleTempLimit = useCallback(async (value: number) => {
    setTuning((prev) => ({ ...prev, temperatureLimit: value }))
    try {
      await gpuApi.setTempLimit(value)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置温度限制失败')
    }
  }, [])

  const handleDynamicBoost = useCallback(async (level: number) => {
    setTuning((prev) => ({ ...prev, dynamicBoostLevel: level }))
    try {
      await gpuApi.setDynamicBoost(level)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置动态加速失败')
    }
  }, [])

  const handleVoltage = useCallback(async (value: number) => {
    setTuning((prev) => ({ ...prev, voltageOffset: value }))
    try {
      await gpuApi.setVoltage(value)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置电压偏移失败')
    }
  }, [])

  const handleGpuMode = useCallback(async (mode: string) => {
    setGpuMode(mode)
    try {
      if (mode === 'optimized') {
        await gpuApi.setOptimized()
      } else {
        await gpuApi.setMode(mode)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置GPU模式失败')
    }
  }, [])

  const handleReset = useCallback(async () => {
    try {
      setIsResetting(true)
      await gpuApi.reset()
      setTuning({
        coreClockOffset: 0,
        memoryClockOffset: 0,
        powerLimit: 150,
        temperatureLimit: 83,
        dynamicBoostLevel: 0,
        voltageOffset: 0,
      })
      setGpuMode('standard')
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : '重置GPU失败')
    } finally {
      setIsResetting(false)
    }
  }, [])

  const vramUsedPct = live.totalVramMB > 0 ? (live.usedVramMB / live.totalVramMB) * 100 : 0

  if (isLoading) {
    return (
      <PageWrapper title="GPU调优" subtitle="GPU超频、功耗和电压控制">
        <div className="gpu-loading">加载GPU状态中...</div>
      </PageWrapper>
    )
  }

  return (
    <PageWrapper
      title="GPU调优"
      subtitle="GPU超频、功耗限制、温度控制和电压调节"
      actions={
        <div className="gpu-actions">
          <button
            className="btn-ghost"
            onClick={() => loadState()}
            disabled={isRefreshing}
          >
            {isRefreshing ? '刷新中...' : '刷新'}
          </button>
          <button
            className="btn-ghost gpu-reset-btn"
            onClick={handleReset}
            disabled={isResetting}
          >
            {isResetting ? '重置中...' : '重置默认'}
          </button>
        </div>
      }
    >
      {error && (
        <div className="gpu-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <Reveal direction="fade" duration={400}>
        <GlowCard title="GPU实时状态" glowColor="primary">
          <div className="gpu-live-stats">
            <div className="gpu-stat-item">
              <span className="gpu-stat-label">温度</span>
              <span className="gpu-stat-value live-value">{live.temperature.toFixed(1)}°C</span>
            </div>
            <div className="gpu-stat-item">
              <span className="gpu-stat-label">热点温度</span>
              <span className="gpu-stat-value live-value">{live.hotspotTemperature.toFixed(1)}°C</span>
            </div>
            <div className="gpu-stat-item">
              <span className="gpu-stat-label">使用率</span>
              <span className="gpu-stat-value live-value">{live.usage.toFixed(1)}%</span>
            </div>
            <div className="gpu-stat-item">
              <span className="gpu-stat-label">功耗</span>
              <span className="gpu-stat-value live-value">{live.power.toFixed(1)}W</span>
            </div>
            <div className="gpu-stat-item">
              <span className="gpu-stat-label">核心时钟</span>
              <span className="gpu-stat-value live-value">{live.coreClockMHz.toFixed(0)} MHz</span>
            </div>
            <div className="gpu-stat-item">
              <span className="gpu-stat-label">显存时钟</span>
              <span className="gpu-stat-value live-value">{live.memoryClockMHz.toFixed(0)} MHz</span>
            </div>
            <div className="gpu-stat-item gpu-stat-vram">
              <span className="gpu-stat-label">显存</span>
              <span className="gpu-stat-value live-value">
                {live.usedVramMB.toFixed(0)} / {live.totalVramMB.toFixed(0)} MB ({vramUsedPct.toFixed(0)}%)
              </span>
            </div>
          </div>
        </GlowCard>
      </Reveal>

      <div className="gpu-grid-2">
        <Reveal direction="left" delay={80}>
          <GlowCard title="时钟偏移" glowColor="accent">
            <div className="gpu-clock-sliders">
              <SliderControl
                label="核心时钟偏移"
                value={tuning.coreClockOffset}
                min={-200}
                max={200}
                step={10}
                unit=" MHz"
                onChange={handleCoreClock}
                color="accent"
              />
              <div className="gpu-current-value">
                <span>当前偏移: </span>
                <span className="gpu-current-num">
                  {tuning.coreClockOffset >= 0 ? '+' : ''}{tuning.coreClockOffset} MHz
                </span>
              </div>

              <SliderControl
                label="显存时钟偏移"
                value={tuning.memoryClockOffset}
                min={-400}
                max={400}
                step={10}
                unit=" MHz"
                onChange={handleMemoryClock}
                color="accent"
              />
              <div className="gpu-current-value">
                <span>当前偏移: </span>
                <span className="gpu-current-num">
                  {tuning.memoryClockOffset >= 0 ? '+' : ''}{tuning.memoryClockOffset} MHz
                </span>
              </div>
            </div>
          </GlowCard>
        </Reveal>

        <Reveal direction="right" delay={120}>
          <GlowCard title="功耗与温度" glowColor="primary">
            <div className="gpu-power-temp">
              <SliderControl
                label="GPU功耗限制"
                value={tuning.powerLimit}
                min={0}
                max={500}
                step={5}
                unit=" W"
                onChange={handlePowerLimit}
                color="primary"
              />
              <SliderControl
                label="温度限制"
                value={tuning.temperatureLimit}
                min={40}
                max={100}
                step={1}
                unit=" °C"
                onChange={handleTempLimit}
                color="primary"
              />

              <div className="gpu-dynamic-boost">
                <span className="gpu-section-label">动态加速</span>
                <div className="gpu-boost-options">
                  {DYNAMIC_BOOST_OPTIONS.map((opt) => (
                    <button
                      key={opt.value}
                      className={`gpu-boost-btn ${tuning.dynamicBoostLevel === opt.value ? 'gpu-boost-btn--active' : ''}`}
                      onClick={() => handleDynamicBoost(opt.value)}
                    >
                      {opt.label}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </GlowCard>
        </Reveal>
      </div>

      <div className="gpu-grid-2">
        <Reveal direction="up" delay={80}>
          <GlowCard title="电压偏移" glowColor="secondary">
            {isNvidia ? (
              <div className="gpu-voltage-control">
                <SliderControl
                  label="电压偏移 (NVIDIA)"
                  value={tuning.voltageOffset}
                  min={-50}
                  max={50}
                  step={1}
                  unit=" mV"
                  onChange={handleVoltage}
                  color="secondary"
                />
                <div className="gpu-current-value">
                  <span>当前偏移: </span>
                  <span className="gpu-current-num">
                    {tuning.voltageOffset >= 0 ? '+' : ''}{tuning.voltageOffset} mV
                  </span>
                </div>
                <p className="gpu-warning-text">
                  ⚠ 电压调整可能影响稳定性，请谨慎调整
                </p>
              </div>
            ) : (
              <div className="gpu-not-supported">
                <span className="gpu-not-supported-icon">🔒</span>
                <p>电压调节仅支持NVIDIA GPU</p>
                <p className="gpu-not-supported-sub">检测到非NVIDIA设备，此功能不可用</p>
              </div>
            )}
          </GlowCard>
        </Reveal>

        <Reveal direction="up" delay={120}>
          <GlowCard title="GPU模式" glowColor="primary">
            <div className="gpu-mode-selector">
              {GPU_MODES.map((m) => (
                <ModeCard
                  key={m.id}
                  title={m.label}
                  description={m.desc}
                  icon={<span>{m.icon}</span>}
                  selected={gpuMode === m.id}
                  onClick={() => handleGpuMode(m.id)}
                  glowColor={m.color}
                />
              ))}
            </div>
          </GlowCard>
        </Reveal>
      </div>

      <Reveal direction="fade" delay={150}>
        <GlowCard title="GPU温度仪表" glowColor="accent">
          <div className="gpu-gauge-row">
            <Gauge
              config={{
                label: 'GPU温度',
                value: live.temperature,
                max: 100,
                unit: '°C',
                color: '#ff00aa',
              }}
            />
            <Gauge
              config={{
                label: '热点温度',
                value: live.hotspotTemperature,
                max: 110,
                unit: '°C',
                color: '#ff4444',
              }}
            />
            <Gauge
              config={{
                label: 'GPU使用率',
                value: live.usage,
                max: 100,
                unit: '%',
                color: '#00aaff',
              }}
            />
            <Gauge
              config={{
                label: '功耗',
                value: live.power,
                max: 500,
                unit: 'W',
                color: '#00ffaa',
              }}
            />
          </div>
        </GlowCard>
      </Reveal>
    </PageWrapper>
  )
}
