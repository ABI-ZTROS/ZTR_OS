import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ToggleSwitch } from '@/components/common/ToggleSwitch'
import { Reveal } from '@/components/common/Reveal'
import { screenApi } from '@/services/screenApi'
import './Screen.css'

const MINILED_MODES = [
  { id: 'off', label: '关闭' },
  { id: 'standard', label: '标准' },
  { id: 'advanced', label: '高级' },
]

const BRIGHTNESS_LEVELS = [
  { level: 0, label: '关闭', icon: '◯' },
  { level: 1, label: '弱光', icon: '◔' },
  { level: 2, label: '中等', icon: '◕' },
  { level: 3, label: '强光', icon: '●' },
]

export function Screen() {
  const [currentRefreshRate, setCurrentRefreshRate] = useState(0)
  const [supportedRates, setSupportedRates] = useState<number[]>([])
  const [overdrive, setOverdrive] = useState(false)
  const [miniLedMode, setMiniLedMode] = useState('off')
  const [hdr, setHdr] = useState(false)
  const [optimalBrightness, setOptimalBrightness] = useState(false)
  const [keyboardBrightness, setKeyboardBrightness] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadState = useCallback(async () => {
    try {
      setIsRefreshing(true)
      const [refreshRes, overdriveRes, miniLedRes, hdrRes, brightnessRes, kbBrightRes] =
        await Promise.allSettled([
          screenApi.getRefreshRate(),
          screenApi.getOverdrive(),
          screenApi.getMiniLed(),
          screenApi.getHdr(),
          screenApi.getOptimalBrightness(),
          screenApi.getKeyboardBrightness(),
        ])

      if (refreshRes.status === 'fulfilled' && refreshRes.value.success) {
        setCurrentRefreshRate(refreshRes.value.data.current)
        setSupportedRates(refreshRes.value.data.supported)
      }

      if (overdriveRes.status === 'fulfilled' && overdriveRes.value.success) {
        setOverdrive(overdriveRes.value.data.enabled)
      }

      if (miniLedRes.status === 'fulfilled' && miniLedRes.value.success) {
        setMiniLedMode(miniLedRes.value.data.mode?.toLowerCase() ?? 'off')
      }

      if (hdrRes.status === 'fulfilled' && hdrRes.value.success) {
        setHdr(hdrRes.value.data.enabled)
      }

      if (brightnessRes.status === 'fulfilled' && brightnessRes.value.success) {
        setOptimalBrightness(brightnessRes.value.data.enabled)
      }

      if (kbBrightRes.status === 'fulfilled' && kbBrightRes.value.success) {
        setKeyboardBrightness(kbBrightRes.value.data.level)
      }

      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : '加载屏幕状态失败')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }, [])

  useEffect(() => {
    loadState()
  }, [loadState])

  const handleRefreshRate = useCallback(async (rate: number) => {
    setCurrentRefreshRate(rate)
    try {
      await screenApi.setRefreshRate(rate)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置刷新率失败')
    }
  }, [])

  const handleOverdrive = useCallback(async (enabled: boolean) => {
    setOverdrive(enabled)
    try {
      await screenApi.setOverdrive(enabled)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置Overdrive失败')
    }
  }, [])

  const handleMiniLed = useCallback(async (mode: string) => {
    setMiniLedMode(mode)
    try {
      await screenApi.setMiniLed(mode)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置MiniLED模式失败')
    }
  }, [])

  const handleHdr = useCallback(async (enabled: boolean) => {
    setHdr(enabled)
    try {
      await screenApi.setHdr(enabled)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置HDR失败')
    }
  }, [])

  const handleOptimalBrightness = useCallback(async (enabled: boolean) => {
    setOptimalBrightness(enabled)
    try {
      await screenApi.setOptimalBrightness(enabled)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置最优亮度失败')
    }
  }, [])

  const handleKeyboardBrightness = useCallback(async (level: number) => {
    setKeyboardBrightness(level)
    try {
      await screenApi.setKeyboardBrightness(level)
    } catch (e) {
      setError(e instanceof Error ? e.message : '设置键盘亮度失败')
    }
  }, [])

  if (isLoading) {
    return (
      <PageWrapper title="屏幕控制" subtitle="刷新率、HDR和显示设置">
        <div className="screen-loading">加载屏幕状态中...</div>
      </PageWrapper>
    )
  }

  return (
    <PageWrapper
      title="屏幕控制"
      subtitle="刷新率、HDR、Overdrive和亮度控制"
      actions={
        <button
          className="btn-ghost"
          onClick={() => loadState()}
          disabled={isRefreshing}
        >
          {isRefreshing ? '刷新中...' : '刷新'}
        </button>
      }
    >
      {error && (
        <div className="screen-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <Reveal direction="fade" duration={400}>
        <GlowCard title="刷新率" glowColor="primary">
          <div className="screen-refresh-info">
            <div className="screen-refresh-current">
              <span className="screen-refresh-label">当前刷新率</span>
              <span className="screen-refresh-value">{currentRefreshRate} Hz</span>
            </div>
            <div className="screen-refresh-supported">
              <span className="screen-supported-label">支持的刷新率</span>
              <div className="screen-refresh-buttons">
                {supportedRates.length > 0 ? (
                  supportedRates.map((rate) => (
                    <button
                      key={rate}
                      className={`screen-refresh-btn ${currentRefreshRate === rate ? 'screen-refresh-btn--active' : ''}`}
                      onClick={() => handleRefreshRate(rate)}
                    >
                      {rate} Hz
                    </button>
                  ))
                ) : (
                  <span className="screen-placeholder">无可用刷新率</span>
                )}
              </div>
            </div>
          </div>
        </GlowCard>
      </Reveal>

      <div className="screen-grid-2">
        <Reveal direction="left" delay={80}>
          <GlowCard title="屏幕增强" glowColor="accent">
            <div className="screen-toggle-group">
              <div className="screen-toggle-item">
                <ToggleSwitch
                  checked={overdrive}
                  onChange={handleOverdrive}
                  label="Overdrive"
                  description="减少拖影，提升响应速度"
                  color="accent"
                />
              </div>

              <div className="screen-toggle-item">
                <ToggleSwitch
                  checked={hdr}
                  onChange={handleHdr}
                  label="HDR"
                  description="高动态范围，更广的色域"
                  color="primary"
                />
              </div>

              <div className="screen-toggle-item">
                <ToggleSwitch
                  checked={optimalBrightness}
                  onChange={handleOptimalBrightness}
                  label="最优亮度"
                  description="根据环境自动调节亮度"
                  color="primary"
                />
              </div>
            </div>
          </GlowCard>
        </Reveal>

        <Reveal direction="right" delay={120}>
          <GlowCard title="MiniLED 模式" glowColor="secondary">
            <div className="screen-miniled-modes">
              {MINILED_MODES.map((mode) => (
                <button
                  key={mode.id}
                  className={`screen-miniled-btn ${miniLedMode === mode.id ? 'screen-miniled-btn--active' : ''}`}
                  onClick={() => handleMiniLed(mode.id)}
                >
                  <span className="screen-miniled-dot" />
                  <span>{mode.label}</span>
                </button>
              ))}
            </div>
            <p className="screen-miniled-desc">
              {miniLedMode === 'off' && 'MiniLED背光已关闭'}
              {miniLedMode === 'standard' && '标准MiniLED背光，均衡的分区控制'}
              {miniLedMode === 'advanced' && '高级MiniLED背光，精确的局域调光'}
            </p>
          </GlowCard>
        </Reveal>
      </div>

      <Reveal direction="up" delay={100}>
        <GlowCard title="键盘亮度" glowColor="accent">
          <div className="screen-brightness-control">
            <div className="screen-brightness-levels">
              {BRIGHTNESS_LEVELS.map((item) => (
                <button
                  key={item.level}
                  className={`screen-brightness-btn ${keyboardBrightness === item.level ? 'screen-brightness-btn--active' : ''}`}
                  onClick={() => handleKeyboardBrightness(item.level)}
                >
                  <span className="screen-brightness-icon">{item.icon}</span>
                  <span className="screen-brightness-label">{item.label}</span>
                </button>
              ))}
            </div>
            <div className="screen-brightness-current">
              <span>当前亮度等级: </span>
              <span className="screen-brightness-level">{BRIGHTNESS_LEVELS.find((b) => b.level === keyboardBrightness)?.label ?? '未知'}</span>
            </div>
          </div>
        </GlowCard>
      </Reveal>
    </PageWrapper>
  )
}
