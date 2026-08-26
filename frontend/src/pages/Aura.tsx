import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ModeCard } from '@/components/common/ModeCard'
import { ColorPicker } from '@/components/common/ColorPicker'
import { SliderControl } from '@/components/common/SliderControl'
import { ToggleSwitch } from '@/components/common/ToggleSwitch'
import { auraApi } from '@/services/otherApi'
import type { AuraDevice, AuraEffectType } from '@/types'
import './Aura.css'

const EFFECTS: { id: AuraEffectType; label: string; icon: string; desc: string }[] = [
  { id: 'static', label: '静态', icon: '◼', desc: '纯色' },
  { id: 'breathe', label: '呼吸', icon: '◐', desc: '呼吸闪烁' },
  { id: 'rainbow', label: '彩虹', icon: '🌈', desc: '色彩循环' },
  { id: 'audio', label: '音频', icon: '🎵', desc: '声音感应' },
  { id: 'heatmap', label: '热感应', icon: '🔥', desc: '温度感应' },
  { id: 'wave', label: '波浪', icon: '🌊', desc: '波浪模式' },
  { id: 'ripple', label: '涟漪', icon: '💧', desc: '点击涟漪' },
  { id: 'starry', label: '星光', icon: '✨', desc: '星光闪烁' },
]

const ZONES: { id: string; label: string; icon: string }[] = [
  { id: 'keyboard', label: '键盘', icon: '⌨' },
  { id: 'body', label: '机身', icon: '💻' },
  { id: 'touchpad', label: '触控板', icon: '▭' },
]

export function Aura() {
  const [devices, setDevices] = useState<AuraDevice[]>([])
  const [selectedZone, setSelectedZone] = useState<string>('keyboard')
  const [selectedEffect, setSelectedEffect] = useState<AuraEffectType>('breathe')
  const [selectedColor, setSelectedColor] = useState('#00ffaa')
  const [brightness, setBrightness] = useState(80)
  const [speed, setSpeed] = useState(50)
  const [intensity, setIntensity] = useState(70)
  const [isEnabled, setIsEnabled] = useState(true)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadDevices = useCallback(async () => {
    try {
      setIsLoading(true)
      const res = await auraApi.getDevices()
      if (res.success) {
        const devs = res.data as unknown as AuraDevice[]
        setDevices(Array.isArray(devs) ? devs : [])
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : '加载灯效设备失败')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadDevices()
  }, [loadDevices])

  const applyEffect = useCallback(async (effect: AuraEffectType) => {
    setSelectedEffect(effect)
    try {
      if (isEnabled) {
        await auraApi.setEffect(selectedZone, effect, {
          color: selectedColor,
          brightness,
          speed,
          intensity,
        })
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '应用效果失败')
    }
  }, [selectedColor, brightness, speed, intensity, selectedZone, isEnabled])

  const handleSpeedChange = useCallback(async (value: number) => {
    setSpeed(value)
    try {
      if (isEnabled) {
        await auraApi.setSpeed(selectedZone, value)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置速度失败')
    }
  }, [selectedZone, isEnabled])

  const handleIntensityChange = useCallback(async (value: number) => {
    setIntensity(value)
    try {
      if (isEnabled) {
        await auraApi.setIntensity(selectedZone, value)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置强度失败')
    }
  }, [selectedZone, isEnabled])

  const handleEnableToggle = useCallback(async (enabled: boolean) => {
    setIsEnabled(enabled)
    try {
      await auraApi.setEnable(selectedZone, enabled)
    } catch (e) {
      setError(e instanceof Error ? e.message : '切换灯效失败')
    }
  }, [selectedZone])

  const applyColor = useCallback(async (color: string) => {
    setSelectedColor(color)
    try {
      if (isEnabled) {
        await auraApi.setColor(selectedZone, color)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置颜色失败')
    }
  }, [selectedZone, isEnabled])

  const applyBrightness = useCallback(async (value: number) => {
    setBrightness(value)
    try {
      if (isEnabled) {
        await auraApi.setBrightness(selectedZone, value)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置亮度失败')
    }
  }, [selectedZone, isEnabled])

  const handleSavePreset = useCallback(async () => {
    try {
      const res = await auraApi.savePreset(`${selectedZone}-${selectedEffect}`)
      if (res.success) {
        setError(null)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '保存预设失败')
    }
  }, [selectedZone, selectedEffect])

  const zoneDevices = devices.filter((d) => d.zone === selectedZone || d.type === selectedZone)

  return (
    <PageWrapper
      title="Aura灯效"
      subtitle="控制华硕设备的RGB灯效"
      actions={
        <div className="inline-group">
          <button className="btn-ghost" onClick={loadDevices} disabled={isLoading}>
            {isLoading ? '加载中...' : '刷新'}
          </button>
          <button className="btn-primary" onClick={handleSavePreset}>
            保存预设
          </button>
        </div>
      }
    >
      {error && (
        <div className="aura-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <div className="grid-2">
        <GlowCard title="区域选择" glowColor="primary">
          <ToggleSwitch
            checked={isEnabled}
            onChange={handleEnableToggle}
            label="灯效开关"
            description="所有灯效的主开关"
            color="primary"
          />
          <div className="divider" />
          <div className="zone-selector">
            {ZONES.map((zone) => (
              <button
                key={zone.id}
                className={`zone-card ${selectedZone === zone.id ? 'zone-card--selected' : ''}`}
                onClick={() => setSelectedZone(zone.id)}
              >
                <span className="zone-icon">{zone.icon}</span>
                <span className="zone-label">{zone.label}</span>
                {selectedZone === zone.id && <span className="zone-indicator">●</span>}
              </button>
            ))}
          </div>
        </GlowCard>

        <GlowCard title="设备列表" glowColor="accent">
          {zoneDevices.length > 0 ? (
            <div className="device-list">
              {zoneDevices.map((device) => (
                <div key={device.id} className="device-item">
                  <div className="device-info">
                    <span className="device-name">{device.name}</span>
                    <span className={`chip ${device.currentEffect ? 'chip--active' : ''}`}>
                      {device.currentEffect ?? '空闲'}
                    </span>
                  </div>
                  <div
                    className="device-color-preview"
                    style={{
                      backgroundColor: device.currentColor ?? selectedColor,
                      boxShadow: `0 0 12px ${device.currentColor ?? selectedColor}`,
                    }}
                  />
                </div>
              ))}
            </div>
          ) : (
            <p className="placeholder-text">
              {isLoading ? '正在扫描设备...' : '此区域未找到设备。'}
            </p>
          )}
          {devices.length > 0 && (
            <div className="device-summary">
              <div className="metric-row">
                <span className="metric-label">设备总数</span>
                <span className="metric-value">{devices.length}</span>
              </div>
              <div className="metric-row">
                <span className="metric-label">激活效果</span>
                <span className="metric-value">
                  {devices.filter((d) => d.currentEffect).length}
                </span>
              </div>
            </div>
          )}
        </GlowCard>
      </div>

      <GlowCard title="效果模式" glowColor="secondary">
        <div className="effects-grid">
          {EFFECTS.map((effect) => (
            <ModeCard
              key={effect.id}
              title={effect.label}
              description={effect.desc}
              icon={<span>{effect.icon}</span>}
              selected={selectedEffect === effect.id}
              onClick={() => applyEffect(effect.id)}
              glowColor="secondary"
            />
          ))}
        </div>
      </GlowCard>

      <div className="grid-2">
        <GlowCard title="颜色与亮度" glowColor="accent">
          <div className="color-controls">
            <ColorPicker
              color={selectedColor}
              onChange={applyColor}
              label="颜色"
            />
            <div className="color-preview-large">
              <div
                className="color-preview-box"
                style={{
                  backgroundColor: selectedColor,
                  boxShadow: `0 0 20px ${selectedColor}, inset 0 0 30px ${selectedColor}40`,
                }}
              />
              <span className="color-hex-label">{selectedColor.toUpperCase()}</span>
            </div>
          </div>
          <div className="divider" />
          <SliderControl
            label="亮度"
            value={brightness}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={applyBrightness}
            color="accent"
          />
        </GlowCard>

        <GlowCard title="效果强度" glowColor="primary">
          <SliderControl
            label="速度"
            value={speed}
            min={1}
            max={100}
            step={1}
            unit="%"
            onChange={handleSpeedChange}
            color="primary"
          />
          <div className="divider" />
          <SliderControl
            label="强度"
            value={intensity}
            min={1}
            max={100}
            step={1}
            unit="%"
            onChange={handleIntensityChange}
            color="secondary"
          />
          <div className="divider" />
          <div className="effect-preview">
            <div
              className="effect-preview-box"
              style={{
                background: `linear-gradient(135deg, ${selectedColor}80, ${selectedColor})`,
                animationDuration: `${(100 - speed) * 0.1 + 0.5}s`,
                animationIterationCount: 'infinite',
                animationTimingFunction: 'ease-in-out',
              }}
            />
            <span className="effect-preview-label">实时预览</span>
          </div>
        </GlowCard>
      </div>
    </PageWrapper>
  )
}
