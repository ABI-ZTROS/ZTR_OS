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
  { id: 'static', label: 'Static', icon: '◼', desc: 'Solid color' },
  { id: 'breathe', label: 'Breathe', icon: '◐', desc: 'Pulse in/out' },
  { id: 'rainbow', label: 'Rainbow', icon: '🌈', desc: 'Color cycle' },
  { id: 'audio', label: 'Audio', icon: '🎵', desc: 'Sound reactive' },
  { id: 'heatmap', label: 'Heatmap', icon: '🔥', desc: 'Temp based' },
  { id: 'wave', label: 'Wave', icon: '🌊', desc: 'Wave pattern' },
  { id: 'ripple', label: 'Ripple', icon: '💧', desc: 'Click ripple' },
  { id: 'starry', label: 'Starry', icon: '✨', desc: 'Twinkling' },
]

const ZONES: { id: string; label: string; icon: string }[] = [
  { id: 'keyboard', label: 'Keyboard', icon: '⌨' },
  { id: 'body', label: 'Body', icon: '💻' },
  { id: 'touchpad', label: 'Touchpad', icon: '▭' },
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
      setError(e instanceof Error ? e.message : 'Failed to load Aura devices')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadDevices()
  }, [loadDevices])

  const applyEffect = useCallback(async (effect: AuraEffectType) => {
    setSelectedEffect(effect)
    const params = {
      color: selectedColor,
      brightness,
      speed,
      intensity,
    }
    try {
      if (isEnabled) {
        await auraApi.setEffect(selectedZone, effect, params)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to apply effect')
    }
  }, [selectedColor, brightness, speed, intensity, selectedZone, isEnabled])

  const applyColor = useCallback(async (color: string) => {
    setSelectedColor(color)
    try {
      if (isEnabled) {
        await auraApi.setColor(selectedZone, color)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set color')
    }
  }, [selectedZone, isEnabled])

  const applyBrightness = useCallback(async (value: number) => {
    setBrightness(value)
    try {
      if (isEnabled) {
        await auraApi.setBrightness(selectedZone, value)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set brightness')
    }
  }, [selectedZone, isEnabled])

  const handleSavePreset = useCallback(async () => {
    try {
      const res = await auraApi.savePreset(`${selectedZone}-${selectedEffect}`)
      if (res.success) {
        setError(null)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save preset')
    }
  }, [selectedZone, selectedEffect])

  const zoneDevices = devices.filter((d) => d.zone === selectedZone || d.type === selectedZone)

  return (
    <PageWrapper
      title="Aura Lighting"
      subtitle="Control RGB lighting effects across supported devices"
      actions={
        <div className="inline-group">
          <button className="btn-ghost" onClick={loadDevices} disabled={isLoading}>
            {isLoading ? 'Loading...' : 'Refresh'}
          </button>
          <button className="btn-primary" onClick={handleSavePreset}>
            Save Preset
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
        <GlowCard title="Zone Selection" glowColor="primary">
          <ToggleSwitch
            checked={isEnabled}
            onChange={setIsEnabled}
            label="Aura Enabled"
            description="Master switch for all lighting effects"
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

        <GlowCard title="Devices in Zone" glowColor="accent">
          {zoneDevices.length > 0 ? (
            <div className="device-list">
              {zoneDevices.map((device) => (
                <div key={device.id} className="device-item">
                  <div className="device-info">
                    <span className="device-name">{device.name}</span>
                    <span className={`chip ${device.currentEffect ? 'chip--active' : ''}`}>
                      {device.currentEffect ?? 'Idle'}
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
              {isLoading ? 'Scanning for devices...' : 'No devices found in this zone.'}
            </p>
          )}
          {devices.length > 0 && (
            <div className="device-summary">
              <div className="metric-row">
                <span className="metric-label">Total Devices</span>
                <span className="metric-value">{devices.length}</span>
              </div>
              <div className="metric-row">
                <span className="metric-label">Active Effects</span>
                <span className="metric-value">
                  {devices.filter((d) => d.currentEffect).length}
                </span>
              </div>
            </div>
          )}
        </GlowCard>
      </div>

      <GlowCard title="Lighting Effects" glowColor="secondary">
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
        <GlowCard title="Color & Brightness" glowColor="accent">
          <div className="color-controls">
            <ColorPicker
              color={selectedColor}
              onChange={applyColor}
              label="Color"
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
            label="Brightness"
            value={brightness}
            min={0}
            max={100}
            step={1}
            unit="%"
            onChange={applyBrightness}
            color="accent"
          />
        </GlowCard>

        <GlowCard title="Effect Intensity" glowColor="primary">
          <SliderControl
            label="Speed"
            value={speed}
            min={1}
            max={100}
            step={1}
            unit="%"
            onChange={setSpeed}
            color="primary"
          />
          <div className="divider" />
          <SliderControl
            label="Intensity"
            value={intensity}
            min={1}
            max={100}
            step={1}
            unit="%"
            onChange={setIntensity}
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
            <span className="effect-preview-label">Live Preview</span>
          </div>
        </GlowCard>
      </div>
    </PageWrapper>
  )
}
