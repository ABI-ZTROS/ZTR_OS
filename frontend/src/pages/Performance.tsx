import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ModeCard } from '@/components/common/ModeCard'
import { SliderControl } from '@/components/common/SliderControl'
import { Gauge } from '@/components/common/Gauge'
import { FanCurveEditor } from '@/components/common/FanCurveEditor'
import { Reveal } from '@/components/common/Reveal'
import { useHardwareStore } from '@/store/useHardwareStore'
import { performanceApi } from '@/services/performanceApi'
import type { PerformanceMode, FanCurvePoint } from '@/types'
import './Performance.css'

const MODES: { id: PerformanceMode; label: string; desc: string; icon: string; color: 'primary' | 'secondary' | 'accent' }[] = [
  { id: 'silent', label: '静音模式', desc: '最低风扇噪音', icon: '🤫', color: 'accent' },
  { id: 'balanced', label: '平衡模式', desc: '默认调校', icon: '⚖', color: 'primary' },
  { id: 'turbo', label: '涡轮增压', desc: '最高性能', icon: '🚀', color: 'secondary' },
  { id: 'fullspeed', label: '全速模式', desc: '风扇全开', icon: '💨', color: 'secondary' },
  { id: 'manual', label: '手动模式', desc: '自定义曲线', icon: '🎛', color: 'accent' },
]

export function Performance() {
  const hardware = useHardwareStore((s) => s.hardware)

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
        setSpl(power.spl ?? 0)
        setSppt(power.sppt ?? 0)
        setFppt(power.fppt ?? 0)
      }

      if (curvesRes.status === 'fulfilled' && curvesRes.value.success) {
        const curves = curvesRes.value.data
        if (curves?.cpu && Array.isArray(curves.cpu) && curves.cpu.length > 0) {
          setCpuCurve(curves.cpu.map((speed, i) => ({ temperature: i * 20, speed })))
        }
        if (curves?.gpu && Array.isArray(curves.gpu) && curves.gpu.length > 0) {
          setGpuCurve(curves.gpu.map((speed, i) => ({ temperature: i * 20, speed })))
        }
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : '加载配置失败')
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
      setError(e instanceof Error ? e.message : '设置性能模式失败')
    }
  }, [])

  const handleCpuPower = useCallback(async (value: number) => {
    setCpuPower(value)
    try {
      await performanceApi.setPowerLimit('cpu', value)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置CPU功耗限制失败')
    }
  }, [])

  const handleGpuPower = useCallback(async (value: number) => {
    setGpuPower(value)
    try {
      await performanceApi.setPowerLimit('gpu', value)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置GPU功耗限制失败')
    }
  }, [])

  const handleSpl = useCallback(async (value: number) => {
    setSpl(value)
    try {
      await performanceApi.setAllPowerLimits(value, sppt, fppt)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置SPL失败')
    }
  }, [sppt, fppt])

  const handleSppt = useCallback(async (value: number) => {
    setSppt(value)
    try {
      await performanceApi.setAllPowerLimits(spl, value, fppt)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置sPPT失败')
    }
  }, [spl, fppt])

  const handleFppt = useCallback(async (value: number) => {
    setFppt(value)
    try {
      await performanceApi.setAllPowerLimits(spl, sppt, value)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置fPPT失败')
    }
  }, [spl, sppt])

  const handleCpuCurveChange = useCallback(async (points: FanCurvePoint[]) => {
    setCpuCurve(points)
    try {
      await performanceApi.setFanCurve(0, points.map((p) => p.speed))
    } catch (e) {
      setError(e instanceof Error ? e.message : '保存CPU风扇曲线失败')
    }
  }, [])

  const handleGpuCurveChange = useCallback(async (points: FanCurvePoint[]) => {
    setGpuCurve(points)
    try {
      await performanceApi.setFanCurve(1, points.map((p) => p.speed))
    } catch (e) {
      setError(e instanceof Error ? e.message : '保存GPU风扇曲线失败')
    }
  }, [])

  const cpu = hardware?.cpu
  const gpu = hardware?.gpu
  const fans = hardware?.fans ?? []

  const getNum = (v: unknown, fallback = 0): number => {
    if (typeof v === 'number' && Number.isFinite(v)) {
      if (v < 0 && (v === -1 || v === -2)) return fallback
      return v
    }
    if (typeof v === 'string') {
      const parsed = Number(v)
      if (Number.isFinite(parsed)) {
        if (parsed < 0 && (parsed === -1 || parsed === -2)) return fallback
        return parsed
      }
    }
    return fallback
  }

  return (
    <PageWrapper
      title="性能控制"
      subtitle="CPU/GPU功耗限制、风扇曲线和GPU性能模式"
      actions={
        <button className="btn-ghost" onClick={loadConfig} disabled={isLoading}>
          {isLoading ? '加载中...' : '刷新'}
        </button>
      }
    >
      {error && (
        <div className="performance-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <Reveal direction="fade" duration={400}>
        <GlowCard title="性能模式" glowColor="primary">
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
      </Reveal>

      <div className="grid-2">
        <Reveal direction="left" delay={80}>
          <GlowCard title="CPU遥测" glowColor="accent">
            <div className="gauge-row">
              <Gauge
                config={{
                  label: 'CPU温度',
                  value: getNum(cpu?.temperature),
                  max: 100,
                  unit: '°C',
                  color: '#00aaff',
                }}
              />
              <Gauge
                config={{
                  label: 'CPU功耗',
                  value: getNum(cpu?.powerDraw),
                  max: 250,
                  unit: 'W',
                  color: '#00ffaa',
                }}
              />
              <Gauge
                config={{
                  label: 'CPU使用率',
                  value: getNum(cpu?.usage),
                  max: 100,
                  unit: '%',
                  color: '#ffaa00',
                }}
              />
            </div>
          </GlowCard>
        </Reveal>

        <Reveal direction="right" delay={120}>
          <GlowCard title="GPU遥测" glowColor="secondary">
            <div className="gauge-row">
              <Gauge
                config={{
                  label: 'GPU温度',
                  value: getNum(gpu?.temperature),
                  max: 100,
                  unit: '°C',
                  color: '#ff00aa',
                }}
              />
              <Gauge
                config={{
                  label: 'GPU功耗',
                  value: getNum(gpu?.powerDraw),
                  max: 450,
                  unit: 'W',
                  color: '#00ffaa',
                }}
              />
              <Gauge
                config={{
                  label: 'GPU使用率',
                  value: getNum(gpu?.usage),
                  max: 100,
                  unit: '%',
                  color: '#00aaff',
                }}
              />
            </div>
          </GlowCard>
        </Reveal>
      </div>

      <Reveal direction="up" delay={100}>
        <GlowCard title="功耗限制" glowColor="accent">
        <div className="power-limits-grid">
          <SliderControl
            label="CPU功耗限制"
            value={cpuPower}
            min={10}
            max={300}
            step={5}
            unit="W"
            onChange={handleCpuPower}
            color="accent"
          />
          <SliderControl
            label="GPU功耗限制"
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
            label="SPL (智能功耗限制)"
            value={spl}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={handleSpl}
            color="primary"
          />
          <SliderControl
            label="sPPT (智能功耗推送)"
            value={sppt}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={handleSppt}
            color="accent"
          />
          <SliderControl
            label="fPPT (风扇功耗推送)"
            value={fppt}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={handleFppt}
            color="secondary"
          />
        </div>
      </GlowCard>
      </Reveal>

      {mode === 'manual' && (
        <div className="grid-2">
          <Reveal direction="up" delay={80}>
            <GlowCard title="CPU风扇曲线" glowColor="accent">
              {cpuCurve.length >= 2 ? (
                <FanCurveEditor
                  points={cpuCurve}
                  onChange={handleCpuCurveChange}
                  label="CPU风扇响应"
                />
              ) : (
                <p className="placeholder-text">暂无CPU风扇曲线数据，请连接后端加载。</p>
              )}
            </GlowCard>
          </Reveal>

          <Reveal direction="up" delay={120}>
            <GlowCard title="GPU风扇曲线" glowColor="primary">
              {gpuCurve.length >= 2 ? (
                <FanCurveEditor
                  points={gpuCurve}
                  onChange={handleGpuCurveChange}
                  label="GPU风扇响应"
                />
              ) : (
                <p className="placeholder-text">暂无GPU风扇曲线数据，请连接后端加载。</p>
              )}
            </GlowCard>
          </Reveal>
        </div>
      )}

      <Reveal direction="fade" delay={150}>
        <GlowCard title="已连接风扇" glowColor="primary">
        {fans.length > 0 ? (
          <div className="fans-list">
            {fans.map((fan, index) => (
              <div key={index} className="fan-item">
                <span className="fan-name">{fan.name ?? `风扇 ${index + 1}`}</span>
                <div className="fan-bar">
                  <div
                    className="fan-bar-fill"
                    style={{ width: `${fan.speed ?? 0}%` }}
                  />
                </div>
                <span className="fan-speed">{fan.speed ?? 0}%</span>
              </div>
            ))}
          </div>
        ) : (
          <p className="placeholder-text">未检测到风扇</p>
        )}
      </GlowCard>
      </Reveal>
    </PageWrapper>
  )
}
