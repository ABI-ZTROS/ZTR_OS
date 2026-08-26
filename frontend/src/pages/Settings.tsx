import { useState, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ToggleSwitch } from '@/components/common/ToggleSwitch'
import { SliderControl } from '@/components/common/SliderControl'
import { useSettingsStore } from '@/store/useSettingsStore'
import { settingsApi } from '@/services/diagnostics'
import type { HotkeyBinding } from '@/types'
import './Settings.css'

const DEFAULT_HOTKEYS: HotkeyBinding[] = [
  { id: '1', action: 'Toggle Performance', keys: ['Ctrl', 'Shift', 'P'] },
  { id: '2', action: 'Toggle Silent Mode', keys: ['Ctrl', 'Shift', 'S'] },
  { id: '3', action: 'Boost CPU', keys: ['Ctrl', 'Alt', 'C'] },
  { id: '4', action: 'Toggle Aura', keys: ['Ctrl', 'Shift', 'A'] },
  { id: '5', action: 'Open Dashboard', keys: ['Ctrl', 'Shift', 'D'] },
]

export function Settings() {
  const settings = useSettingsStore((s) => s.settings)
  const updateSettings = useSettingsStore((s) => s.updateSettings)
  const resetSettings = useSettingsStore((s) => s.resetSettings)

  const [activeTab, setActiveTab] = useState<'general' | 'mlp' | 'hotkeys' | 'hardware' | 'about'>('general')
  const [hotkeys, setHotkeys] = useState<HotkeyBinding[]>(DEFAULT_HOTKEYS)
  const [exportStatus, setExportStatus] = useState<string | null>(null)

  const syncSetting = useCallback(async (key: keyof typeof settings, value: unknown) => {
    const patch: Record<string, unknown> = {}
    patch[key] = value
    try {
      await settingsApi.updateUserSettings(patch)
    } catch {
      // silently fail - frontend still updates
    }
  }, [])

  const handleExport = useCallback(() => {
    try {
      const config = JSON.stringify(settings, null, 2)
      const blob = new Blob([config], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'ztr-settings.json'
      a.click()
      URL.revokeObjectURL(url)
      setExportStatus('Settings exported successfully')
      setTimeout(() => setExportStatus(null), 3000)
    } catch {
      setExportStatus('Export failed')
      setTimeout(() => setExportStatus(null), 3000)
    }
  }, [settings])

  const handleImport = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = (event) => {
      try {
        const imported = JSON.parse(event.target?.result as string)
        updateSettings(imported)
        setExportStatus('Settings imported successfully')
        setTimeout(() => setExportStatus(null), 3000)
      } catch {
        setExportStatus('Import failed - invalid file')
        setTimeout(() => setExportStatus(null), 3000)
      }
    }
    reader.readAsText(file)
    e.target.value = ''
  }, [updateSettings])

  const tabs: { id: typeof activeTab; label: string }[] = [
    { id: 'general', label: 'General' },
    { id: 'mlp', label: 'MLP' },
    { id: 'hotkeys', label: 'Hotkeys' },
    { id: 'hardware', label: 'Hardware' },
    { id: 'about', label: 'About' },
  ]

  return (
    <PageWrapper
      title="Settings"
      subtitle="Configure ZTR_OS behavior and preferences"
      actions={
        <button className="btn-ghost" onClick={resetSettings}>
          Reset to Defaults
        </button>
      }
    >
      {exportStatus && (
        <div className={`settings-notification ${exportStatus.includes('failed') ? 'settings-notification--error' : 'settings-notification--success'}`}>
          {exportStatus}
        </div>
      )}

      <div className="settings-tabs">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            className={`settings-tab ${activeTab === tab.id ? 'settings-tab--active' : ''}`}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === 'general' && (
        <>
          <GlowCard title="General" glowColor="primary">
            <ToggleSwitch
              checked={settings.autoPerformance}
              onChange={(v) => { updateSettings({ autoPerformance: v }); syncSetting('autoPerformance', v) }}
              label="Auto Performance"
              description="Automatically adjust power limits based on system load"
              color="primary"
            />
            <ToggleSwitch
              checked={settings.autoMlp}
              onChange={(v) => { updateSettings({ autoMlp: v }); syncSetting('autoMlp', v) }}
              label="Auto MLP"
              description="Enable machine-learning-based performance decisions"
              color="accent"
            />
            <ToggleSwitch
              checked={settings.autoAura}
              onChange={(v) => { updateSettings({ autoAura: v }); syncSetting('autoAura', v) }}
              label="Auto Aura"
              description="Dynamic lighting effects based on system state"
              color="secondary"
            />
            <div className="divider" />
            <ToggleSwitch
              checked={settings.autoStart}
              onChange={(v) => updateSettings({ autoStart: v })}
              label="Auto Start"
              description="Start ZTR_OS when the system boots"
              color="primary"
            />
            <ToggleSwitch
              checked={settings.minimizeToTray}
              onChange={(v) => updateSettings({ minimizeToTray: v })}
              label="Minimize to Tray"
              description="Minimize to system tray instead of taskbar"
              color="accent"
            />
          </GlowCard>

          <GlowCard title="Interface" glowColor="accent">
            <div className="setting-row">
              <div>
                <div className="setting-label">Theme</div>
                <div className="setting-desc">Visual theme for the application</div>
              </div>
              <select
                className="form-select"
                value={settings.theme}
                onChange={(e) => updateSettings({ theme: e.target.value as 'dark' | 'cyber' })}
              >
                <option value="cyber">Cyber Neon</option>
                <option value="dark">Dark Classic</option>
              </select>
            </div>
            <div className="setting-row">
              <div>
                <div className="setting-label">Notifications</div>
                <div className="setting-desc">Show desktop notifications for important events</div>
              </div>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={settings.notificationsEnabled}
                  onChange={(e) => updateSettings({ notificationsEnabled: e.target.checked })}
                />
                <span className="switch-slider" />
              </label>
            </div>
            <div className="setting-row">
              <div>
                <div className="setting-label">Polling Interval</div>
                <div className="setting-desc">How often to fetch hardware data (ms)</div>
              </div>
              <input
                type="number"
                min={500}
                step={500}
                value={settings.pollingInterval}
                onChange={(e) => updateSettings({ pollingInterval: Number(e.target.value) })}
                className="number-input"
              />
            </div>
          </GlowCard>
        </>
      )}

      {activeTab === 'mlp' && (
        <>
          <GlowCard title="MLP Settings" glowColor="accent">
            <SliderControl
              label="Default Learning Rate"
              value={0.001}
              min={0.0001}
              max={0.01}
              step={0.0001}
              onChange={() => {}}
              color="accent"
              formatValue={(v) => v.toFixed(4)}
            />
            <div className="divider" />
            <ToggleSwitch
              checked={settings.autoModeSwitch}
              onChange={(v) => updateSettings({ autoModeSwitch: v })}
              label="Auto Mode Switch"
              description="Automatically switch performance modes based on MLP predictions"
              color="primary"
            />
            <SliderControl
              label="Prediction Window"
              value={settings.predictionWindow}
              min={10}
              max={200}
              step={10}
              unit=" decisions"
              onChange={(v) => updateSettings({ predictionWindow: v })}
              color="accent"
            />
          </GlowCard>

          <GlowCard title="MLP Model Defaults" glowColor="primary">
            <div className="metric-row">
              <span className="metric-label">Default Hidden Layers</span>
              <span className="metric-value">[64, 32, 16]</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">Default Input Size</span>
              <span className="metric-value">10</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">Default Output Size</span>
              <span className="metric-value">4</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">Status</span>
              <span className="metric-value">
                <span className="chip chip--active">Configured</span>
              </span>
            </div>
          </GlowCard>
        </>
      )}

      {activeTab === 'hotkeys' && (
        <GlowCard title="Hotkey Bindings" glowColor="primary">
          <div className="hotkey-list">
            {hotkeys.map((hotkey, index) => (
              <div key={hotkey.id} className="hotkey-item">
                <span className="hotkey-action">{hotkey.action}</span>
                <div className="hotkey-keys">
                  {hotkey.keys.map((key, keyIndex) => (
                    <span key={`${hotkey.id}-${keyIndex}`} className="hotkey-key">
                      {key}
                      {keyIndex < hotkey.keys.length - 1 && (
                        <span className="hotkey-plus">+</span>
                      )}
                    </span>
                  ))}
                </div>
                <button
                  className="btn-ghost"
                  onClick={() => {
                    const newKeys = prompt(`Edit hotkey for "${hotkey.action}" (comma separated):`, hotkey.keys.join(','))
                    if (newKeys) {
                      setHotkeys((prev) => {
                        const updated = [...prev]
                        updated[index] = { ...hotkey, keys: newKeys.split(',').map((k) => k.trim()) }
                        return updated
                      })
                    }
                  }}
                >
                  Edit
                </button>
              </div>
            ))}
          </div>
        </GlowCard>
      )}

      {activeTab === 'hardware' && (
        <>
          <GlowCard title="Connection" glowColor="none">
            <div className="setting-row">
              <div>
                <div className="setting-label">API URL</div>
                <div className="setting-desc">Backend API endpoint (configured via VITE_API_URL)</div>
              </div>
              <code className="api-url">{import.meta.env.VITE_API_URL ?? 'Not set'}</code>
            </div>
          </GlowCard>

          <GlowCard title="Hardware-Specific" glowColor="accent">
            <div className="metric-row">
              <span className="metric-label">CPU Architecture</span>
              <span className="metric-value">
                {navigator.hardwareConcurrency > 0
                  ? `${navigator.hardwareConcurrency} logical cores`
                  : 'Unknown'}
              </span>
            </div>
            <div className="metric-row">
              <span className="metric-label">Platform</span>
              <span className="metric-value">{navigator.platform ?? 'Unknown'}</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">GPU Renderer</span>
              <span className="metric-value">
                <canvas id="gpu-canvas" style={{ display: 'none' }} />
              </span>
            </div>
          </GlowCard>

          <GlowCard title="Configuration Management" glowColor="primary">
            <div className="inline-group">
              <button className="btn-primary" onClick={handleExport}>
                Export Configuration
              </button>
              <label className="btn-ghost" style={{ cursor: 'pointer' }}>
                Import Configuration
                <input
                  type="file"
                  accept=".json"
                  onChange={handleImport}
                  style={{ display: 'none' }}
                />
              </label>
            </div>
          </GlowCard>
        </>
      )}

      {activeTab === 'about' && (
        <GlowCard title="About ZTR_OS" glowColor="primary">
          <div className="about-content">
            <div className="about-logo">
              <span className="about-logo-icon">ZTR</span>
              <span className="about-logo-sub">OS</span>
            </div>
            <div className="about-version">
              <span className="version-label">Version</span>
              <span className="version-value">1.0.0</span>
            </div>
            <div className="about-desc">
              ZTR_OS is an advanced system management and optimization platform that provides
              real-time hardware monitoring, AI-powered performance tuning, and dynamic
              lighting control for your device.
            </div>
            <div className="about-features">
              <div className="about-feature">
                <span className="feature-icon">⚡</span>
                <span>Real-time CPU/GPU power management</span>
              </div>
              <div className="about-feature">
                <span className="feature-icon">🧠</span>
                <span>ML-powered adaptive tuning</span>
              </div>
              <div className="about-feature">
                <span className="feature-icon">🎨</span>
                <span>Dynamic RGB lighting control</span>
              </div>
              <div className="about-feature">
                <span className="feature-icon">🔗</span>
                <span>Process affinity binding</span>
              </div>
            </div>
            <div className="about-links">
              <a href="#" className="about-link">Documentation</a>
              <a href="#" className="about-link">GitHub</a>
              <a href="#" className="about-link">Support</a>
            </div>
          </div>
        </GlowCard>
      )}
    </PageWrapper>
  )
}
