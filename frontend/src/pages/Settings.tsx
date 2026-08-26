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
  { id: '1', action: '切换性能', keys: ['Ctrl', 'Shift', 'P'] },
  { id: '2', action: '切换静音模式', keys: ['Ctrl', 'Shift', 'S'] },
  { id: '3', action: '加速 CPU', keys: ['Ctrl', 'Alt', 'C'] },
  { id: '4', action: '切换灯效', keys: ['Ctrl', 'Shift', 'A'] },
  { id: '5', action: '打开仪表板', keys: ['Ctrl', 'Shift', 'D'] },
]

const DEFAULT_SETTINGS = {
  autoPerformance: true,
  autoMlp: true,
  autoAura: true,
  pollingInterval: 2000,
  theme: 'cyber',
  notificationsEnabled: true,
  autoStart: false,
  minimizeToTray: true,
  predictionWindow: 50,
  autoModeSwitch: true,
}

export function Settings() {
  const settings = useSettingsStore((s) => s.settings)
  const updateSettings = useSettingsStore((s) => s.updateSettings)
  const resetSettings = useSettingsStore((s) => s.resetSettings)

  const [activeTab, setActiveTab] = useState<'general' | 'mlp' | 'hotkeys' | 'hardware' | 'about'>('general')
  const [hotkeys, setHotkeys] = useState<HotkeyBinding[]>(DEFAULT_HOTKEYS)
  const [exportStatus, setExportStatus] = useState<string | null>(null)

  const syncSetting = useCallback(async (patch: Record<string, unknown>) => {
    try {
      await settingsApi.updateUserSettings(patch)
    } catch {
      // silently fail
    }
  }, [])

  const handleBooleanToggle = useCallback(async (key: string, value: boolean) => {
    const patch: Record<string, unknown> = {}
    patch[key] = value
    updateSettings({ [key]: value } as Parameters<typeof updateSettings>[0])
    await syncSetting(patch)
  }, [updateSettings, syncSetting])

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
      setExportStatus('设置导出成功')
      setTimeout(() => setExportStatus(null), 3000)
    } catch {
      setExportStatus('导出失败')
      setTimeout(() => setExportStatus(null), 3000)
    }
  }, [settings])

  const handleImport = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = async (event) => {
      try {
        const imported = JSON.parse(event.target?.result as string)
        updateSettings(imported)
        await syncSetting(imported)
        setExportStatus('设置导入成功')
        setTimeout(() => setExportStatus(null), 3000)
      } catch {
        setExportStatus('导入失败 - 文件无效')
        setTimeout(() => setExportStatus(null), 3000)
      }
    }
    reader.readAsText(file)
    e.target.value = ''
  }, [updateSettings, syncSetting])

  const handleResetToDefaults = useCallback(async () => {
    resetSettings()
    await syncSetting(DEFAULT_SETTINGS)
    setExportStatus('设置已恢复默认')
    setTimeout(() => setExportStatus(null), 3000)
  }, [resetSettings, syncSetting])

  const handleHotkeyEdit = useCallback(async (index: number, newKeysStr: string) => {
    const newKeys = newKeysStr.split(',').map((k) => k.trim())
    setHotkeys((prev) => {
      const updated = [...prev]
      updated[index] = { ...updated[index], keys: newKeys }
      return updated
    })
    const patch = { hotkeys: hotkeys.map((h, i) => i === index ? { ...h, keys: newKeys } : h) }
    await syncSetting(patch)
  }, [hotkeys, syncSetting])

  const handleLearningRateDefaultChange = useCallback(async (v: number) => {
    // This is just updating the display; actual MLP learning rate is on MLP page
    // We'll store it as a custom property for reference
    setExportStatus(`默认学习率：${v.toFixed(4)}`)
    setTimeout(() => setExportStatus(null), 2000)
  }, [])

  const tabs: { id: typeof activeTab; label: string }[] = [
    { id: 'general', label: '通用' },
    { id: 'mlp', label: '机器学习' },
    { id: 'hotkeys', label: '快捷键' },
    { id: 'hardware', label: '硬件' },
    { id: 'about', label: '关于' },
  ]

  return (
    <PageWrapper
      title="设置"
      subtitle="配置 ZTR_OS 行为和偏好"
      actions={
        <button className="btn-ghost" onClick={handleResetToDefaults}>
          恢复默认
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
          <GlowCard title="通用设置" glowColor="primary">
            <ToggleSwitch
              checked={settings.autoPerformance}
              onChange={(v) => handleBooleanToggle('autoPerformance', v)}
              label="自动性能"
              description="根据系统负载自动调整功率限制"
              color="primary"
            />
            <ToggleSwitch
              checked={settings.autoMlp}
              onChange={(v) => handleBooleanToggle('autoMlp', v)}
              label="自动机器学习"
              description="启用基于机器学习的性能决策"
              color="accent"
            />
            <ToggleSwitch
              checked={settings.autoAura}
              onChange={(v) => handleBooleanToggle('autoAura', v)}
              label="自动灯效"
              description="根据系统状态实现动态灯效"
              color="secondary"
            />
            <div className="divider" />
            <ToggleSwitch
              checked={settings.autoStart}
              onChange={(v) => handleBooleanToggle('autoStart', v)}
              label="开机自启"
              description="系统启动时自动启动 ZTR_OS"
              color="primary"
            />
            <ToggleSwitch
              checked={settings.minimizeToTray}
              onChange={(v) => handleBooleanToggle('minimizeToTray', v)}
              label="最小化到托盘"
              description="最小化到系统托盘而非任务栏"
              color="accent"
            />
          </GlowCard>

          <GlowCard title="界面设置" glowColor="accent">
            <div className="setting-row">
              <div>
                <div className="setting-label">主题</div>
                <div className="setting-desc">应用程序的视觉主题</div>
              </div>
              <select
                className="form-select"
                value={settings.theme}
                onChange={(e) => {
                  const theme = e.target.value as 'dark' | 'cyber'
                  updateSettings({ theme })
                  syncSetting({ theme })
                }}
              >
                <option value="cyber">赛博霓虹</option>
                <option value="dark">暗夜经典</option>
              </select>
            </div>
            <div className="setting-row">
              <div>
                <div className="setting-label">通知</div>
                <div className="setting-desc">为重要事件显示桌面通知</div>
              </div>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={settings.notificationsEnabled}
                  onChange={(e) => handleBooleanToggle('notificationsEnabled', e.target.checked)}
                />
                <span className="switch-slider" />
              </label>
            </div>
            <div className="setting-row">
              <div>
                <div className="setting-label">轮询间隔</div>
                <div className="setting-desc">获取硬件数据的频率（毫秒）</div>
              </div>
              <input
                type="number"
                min={500}
                step={500}
                value={settings.pollingInterval}
                onChange={(e) => {
                  const val = Number(e.target.value)
                  updateSettings({ pollingInterval: val })
                  syncSetting({ pollingInterval: val })
                }}
                className="number-input"
              />
            </div>
          </GlowCard>
        </>
      )}

      {activeTab === 'mlp' && (
        <>
          <GlowCard title="机器学习设置" glowColor="accent">
            <SliderControl
              label="默认学习率"
              value={0.001}
              min={0.0001}
              max={0.01}
              step={0.0001}
              onChange={handleLearningRateDefaultChange}
              color="accent"
              formatValue={(v) => v.toFixed(4)}
            />
            <div className="divider" />
            <ToggleSwitch
              checked={settings.autoModeSwitch}
              onChange={(v) => handleBooleanToggle('autoModeSwitch', v)}
              label="自动模式切换"
              description="根据机器学习预测自动切换性能模式"
              color="primary"
            />
            <SliderControl
              label="预测窗口"
              value={settings.predictionWindow}
              min={10}
              max={200}
              step={10}
              unit=" 决策"
              onChange={(v) => {
                updateSettings({ predictionWindow: v })
                syncSetting({ predictionWindow: v })
              }}
              color="accent"
            />
          </GlowCard>

          <GlowCard title="机器学习模型默认值" glowColor="primary">
            <div className="metric-row">
              <span className="metric-label">默认隐藏层</span>
              <span className="metric-value">[64, 32, 16]</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">默认输入大小</span>
              <span className="metric-value">10</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">默认输出大小</span>
              <span className="metric-value">4</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">状态</span>
              <span className="metric-value">
                <span className="chip chip--active">已配置</span>
              </span>
            </div>
          </GlowCard>
        </>
      )}

      {activeTab === 'hotkeys' && (
        <GlowCard title="快捷键绑定" glowColor="primary">
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
                    const newKeys = prompt(`编辑快捷键（逗号分隔）:`, hotkey.keys.join(','))
                    if (newKeys) {
                      handleHotkeyEdit(index, newKeys)
                    }
                  }}
                >
                  编辑
                </button>
              </div>
            ))}
          </div>
        </GlowCard>
      )}

      {activeTab === 'hardware' && (
        <>
          <GlowCard title="连接" glowColor="none">
            <div className="setting-row">
              <div>
                <div className="setting-label">API URL</div>
                <div className="setting-desc">后端 API 端点（通过 VITE_API_URL 配置）</div>
              </div>
              <code className="api-url">{import.meta.env.VITE_API_URL ?? '未设置'}</code>
            </div>
          </GlowCard>

          <GlowCard title="硬件信息" glowColor="accent">
            <div className="metric-row">
              <span className="metric-label">CPU 架构</span>
              <span className="metric-value">
                {navigator.hardwareConcurrency > 0
                  ? `${navigator.hardwareConcurrency} 逻辑核心`
                  : '未知'}
              </span>
            </div>
            <div className="metric-row">
              <span className="metric-label">平台</span>
              <span className="metric-value">{navigator.platform ?? '未知'}</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">GPU 渲染器</span>
              <span className="metric-value">
                <canvas id="gpu-canvas" style={{ display: 'none' }} />
              </span>
            </div>
          </GlowCard>

          <GlowCard title="配置管理" glowColor="primary">
            <div className="inline-group">
              <button className="btn-primary" onClick={handleExport}>
                导出配置
              </button>
              <label className="btn-ghost" style={{ cursor: 'pointer' }}>
                导入配置
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
        <GlowCard title="关于 ZTR_OS" glowColor="primary">
          <div className="about-content">
            <div className="about-logo">
              <span className="about-logo-icon">ZTR</span>
              <span className="about-logo-sub">OS</span>
            </div>
            <div className="about-version">
              <span className="version-label">版本</span>
              <span className="version-value">1.0.0</span>
            </div>
            <div className="about-desc">
              ZTR_OS 是一款先进的系统管理和优化平台，提供实时硬件监控、AI 驱动的性能调优以及设备动态灯效控制。
            </div>
            <div className="about-features">
              <div className="about-feature">
                <span className="feature-icon">⚡</span>
                <span>实时 CPU/GPU 功率管理</span>
              </div>
              <div className="about-feature">
                <span className="feature-icon">🧠</span>
                <span>机器学习驱动的自适应调优</span>
              </div>
              <div className="about-feature">
                <span className="feature-icon">🎨</span>
                <span>动态 RGB 灯效控制</span>
              </div>
              <div className="about-feature">
                <span className="feature-icon">🔗</span>
                <span>处理器亲和性绑定</span>
              </div>
            </div>
            <div className="about-links">
              <a href="#" className="about-link">文档</a>
              <a href="#" className="about-link">GitHub</a>
              <a href="#" className="about-link">支持</a>
            </div>
          </div>
        </GlowCard>
      )}
    </PageWrapper>
  )
}
