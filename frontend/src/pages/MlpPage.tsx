import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ToggleSwitch } from '@/components/common/ToggleSwitch'
import { SliderControl } from '@/components/common/SliderControl'
import { Timeline } from '@/components/common/Timeline'
import { useMlpStore } from '@/store/useMlpStore'
import { mlpApi, type MlpConfigResponse } from '@/services/mlpApi'
import { performanceApi } from '@/services/performanceApi'
import type { TimelineEvent, MlpConfig } from '@/types'
import './MlpPage.css'

function mapConfigToState(config: MlpConfigResponse): MlpConfig {
  return {
    learningRate: config.learningRate,
    hiddenLayers: config.hiddenLayers,
    inputSize: config.inputSize,
    outputSize: config.outputSize,
    isTraining: config.isTraining,
    epochs: config.epochs,
    currentEpoch: config.currentEpoch,
    loss: config.loss,
  }
}

export function MlpPage() {
  const mlpState = useMlpStore((s) => s.state)
  const decisions = useMlpStore((s) => s.decisions)
  const updateState = useMlpStore((s) => s.updateState)

  const [isEnabled, setIsEnabled] = useState(false)
  const [learningRate, setLearningRate] = useState(0.001)
  const [hiddenLayersStr, setHiddenLayersStr] = useState('64,32,16')
  const [inputSize, setInputSize] = useState(10)
  const [outputSize, setOutputSize] = useState(4)
  const [overrideAction, setOverrideAction] = useState('')
  const [events, setEvents] = useState<TimelineEvent[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadConfig = useCallback(async () => {
    try {
      setIsLoading(true)
      const res = await mlpApi.getConfig()
      if (res.success && res.data) {
        const config = res.data
        setLearningRate(config.learningRate)
        setHiddenLayersStr(config.hiddenLayers.join(','))
        setInputSize(config.inputSize)
        setOutputSize(config.outputSize)
        setIsEnabled(config.isTraining)
        updateState({ config: mapConfigToState(config) })
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load MLP config')
    } finally {
      setIsLoading(false)
    }
  }, [updateState])

  useEffect(() => {
    loadConfig()
  }, [loadConfig])

  useEffect(() => {
    const newEvents: TimelineEvent[] = decisions.map((d) => ({
      id: d.id,
      timestamp: d.timestamp,
      type: 'decision',
      title: d.action,
      description: `Confidence: ${(d.confidence * 100).toFixed(1)}%`,
      metadata: {
        confidence: d.confidence,
        output: d.output.join(', '),
      },
    }))
    setEvents(newEvents)
  }, [decisions])

  const handleToggle = useCallback(async (enabled: boolean) => {
    setIsEnabled(enabled)
    try {
      if (enabled) {
        await mlpApi.startTraining(mlpState.config)
      } else {
        await mlpApi.stopTraining()
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to toggle MLP')
      setIsEnabled(!enabled)
    }
  }, [mlpState.config])

  const handleLearningRateChange = useCallback(async (v: number) => {
    const rounded = Number(v.toFixed(4))
    setLearningRate(rounded)
    try {
      const config = {
        ...mlpState.config,
        learningRate: rounded,
      }
      await mlpApi.setConfig(config as unknown as MlpConfigResponse)
      updateState({ config })
    } catch {
      // silently fail
    }
  }, [mlpState.config, updateState])

  const handleSaveConfig = useCallback(async () => {
    const hiddenLayers = hiddenLayersStr
      .split(',')
      .map((s) => parseInt(s.trim(), 10))
      .filter((n) => !isNaN(n) && n > 0)

    const config: MlpConfig = {
      learningRate,
      hiddenLayers,
      inputSize,
      outputSize,
      isTraining: mlpState.config.isTraining,
      epochs: mlpState.config.epochs,
      currentEpoch: mlpState.config.currentEpoch,
      loss: mlpState.config.loss,
    }

    try {
      const res = await mlpApi.setConfig(config)
      if (res.success) {
        updateState({ config })
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save config')
    }
  }, [learningRate, hiddenLayersStr, inputSize, outputSize, mlpState.config, updateState])

  const handleOverride = useCallback(async () => {
    if (!overrideAction) return
    try {
      const modeMap: Record<string, string> = {
        eco: 'silent',
        balanced: 'balanced',
        performance: 'turbo',
        boost: 'turbo',
        silent: 'silent',
      }
      const mode = modeMap[overrideAction] ?? 'balanced'
      await mlpApi.stopTraining()
      const modeRes = await performanceApi.setGpuMode(mode)
      if (modeRes.success) {
        updateState({
          config: { ...mlpState.config, isTraining: false },
          status: 'idle',
        })
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Override failed')
    }
  }, [overrideAction, mlpState.config, updateState])

  const handleReset = useCallback(async () => {
    try {
      await mlpApi.resetModel()
      updateState({
        config: { ...mlpState.config, epochs: 0, currentEpoch: 0, loss: 0 },
        status: 'idle',
      })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Reset failed')
    }
  }, [mlpState.config, updateState])

  const epochProgress = mlpState.config.epochs > 0
    ? (mlpState.config.currentEpoch / mlpState.config.epochs) * 100
    : 0

  const statusColor = mlpState.status === 'training'
    ? 'chip--warning'
    : mlpState.status === 'running'
      ? 'chip--active'
      : mlpState.status === 'error'
        ? 'chip--danger'
        : ''

  return (
    <PageWrapper
      title="MLP Visualization"
      subtitle="Multi-layer perceptron training and decision tracking"
      actions={
        <div className="inline-group">
          <button className="btn-ghost" onClick={loadConfig} disabled={isLoading}>
            {isLoading ? 'Loading...' : 'Refresh'}
          </button>
          <button className="btn-ghost" onClick={handleReset}>
            Reset Model
          </button>
        </div>
      }
    >
      {error && (
        <div className="mlp-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <div className="grid-2">
        <GlowCard title="MLP Control" glowColor="primary">
          <div className="mlp-control-row">
            <div className="mlp-status-row">
              <span className="status-label">Status</span>
              <span className={`chip ${statusColor}`}>{mlpState.status}</span>
            </div>
            <ToggleSwitch
              checked={isEnabled}
              onChange={handleToggle}
              label="Enable MLP"
              description="Start/stop the multi-layer perceptron engine"
              color="primary"
            />
          </div>
          <div className="divider" />
          <div className="mlp-metrics">
            <div className="metric-row">
              <span className="metric-label">Epoch</span>
              <span className="metric-value">
                {mlpState.config.currentEpoch} / {mlpState.config.epochs}
              </span>
            </div>
            <div className="metric-row">
              <span className="metric-label">Loss</span>
              <span className="metric-value">{mlpState.config.loss.toFixed(4)}</span>
            </div>
            <div className="metric-row">
              <span className="metric-label">Last Updated</span>
              <span className="metric-value">
                {new Date(mlpState.lastUpdated).toLocaleTimeString()}
              </span>
            </div>
            {mlpState.config.epochs > 0 && (
              <div className="epoch-progress">
                <span className="metric-label">Training Progress</span>
                <div className="progress-bar">
                  <div
                    className="progress-bar-fill progress-bar-fill--primary"
                    style={{ width: `${epochProgress}%` }}
                  />
                </div>
                <span className="progress-text">{epochProgress.toFixed(1)}%</span>
              </div>
            )}
          </div>
        </GlowCard>

        <GlowCard title="Prediction Accuracy" glowColor="accent">
          <div className="accuracy-display">
            <div className="accuracy-circle">
              <svg viewBox="0 0 120 120" className="accuracy-svg">
                <circle
                  cx="60"
                  cy="60"
                  r="50"
                  fill="none"
                  stroke="var(--border)"
                  strokeWidth="8"
                />
                <circle
                  cx="60"
                  cy="60"
                  r="50"
                  fill="none"
                  stroke="var(--accent)"
                  strokeWidth="8"
                  strokeDasharray={`${2 * Math.PI * 50}`}
                  strokeDashoffset={`${2 * Math.PI * 50 * (1 - Math.min(mlpState.config.loss * 100, 100) / 100)}`}
                  strokeLinecap="round"
                  transform="rotate(-90 60 60)"
                  style={{ transition: 'stroke-dashoffset 0.5s ease' }}
                />
                <text x="60" y="58" className="accuracy-value" textAnchor="middle">
                  {(Math.max(0, 100 - mlpState.config.loss * 100)).toFixed(1)}%
                </text>
                <text x="60" y="75" className="accuracy-label" textAnchor="middle">
                  Accuracy
                </text>
              </svg>
            </div>
            <div className="accuracy-stats">
              <div className="metric-row">
                <span className="metric-label">Decisions</span>
                <span className="metric-value">{decisions.length}</span>
              </div>
              <div className="metric-row">
                <span className="metric-label">Confidence (avg)</span>
                <span className="metric-value">
                  {decisions.length > 0
                    ? ((decisions.reduce((a, d) => a + d.confidence, 0) / decisions.length) * 100).toFixed(1)
                    : '0.0'}%
                </span>
              </div>
              <div className="metric-row">
                <span className="metric-label">Status</span>
                <span className="metric-value">{mlpState.config.isTraining ? 'Training' : 'Idle'}</span>
              </div>
            </div>
          </div>
        </GlowCard>
      </div>

      <GlowCard title="Model Configuration" glowColor="accent">
        <div className="config-grid">
          <SliderControl
            label="Learning Rate"
            value={learningRate}
            min={0.0001}
            max={0.01}
            step={0.0001}
            onChange={handleLearningRateChange}
            color="accent"
            formatValue={(v) => v.toFixed(4)}
          />
          <div className="form-row">
            <span className="form-label">Hidden Layers (comma separated)</span>
            <input
              type="text"
              className="form-input"
              value={hiddenLayersStr}
              onChange={(e) => setHiddenLayersStr(e.target.value)}
              placeholder="64,32,16"
            />
          </div>
          <div className="form-row">
            <span className="form-label">Input Size</span>
            <input
              type="number"
              className="form-input"
              value={inputSize}
              min={1}
              max={100}
              onChange={(e) => setInputSize(Number(e.target.value))}
            />
          </div>
          <div className="form-row">
            <span className="form-label">Output Size</span>
            <input
              type="number"
              className="form-input"
              value={outputSize}
              min={1}
              max={100}
              onChange={(e) => setOutputSize(Number(e.target.value))}
            />
          </div>
        </div>
        <div className="inline-group" style={{ marginTop: 12 }}>
          <button className="btn-primary" onClick={handleSaveConfig}>
            Save Configuration
          </button>
        </div>
      </GlowCard>

      <div className="grid-2">
        <GlowCard title="Manual Override" glowColor="secondary">
          <div className="form-row">
            <span className="form-label">Override Action</span>
            <select
              className="form-select"
              value={overrideAction}
              onChange={(e) => setOverrideAction(e.target.value)}
            >
              <option value="">Select an action...</option>
              <option value="eco">Eco Mode</option>
              <option value="balanced">Balanced Mode</option>
              <option value="performance">Performance Mode</option>
              <option value="boost">Boost Mode</option>
              <option value="silent">Silent Mode</option>
            </select>
          </div>
          <div className="inline-group" style={{ marginTop: 12 }}>
            <button
              className="btn-secondary"
              onClick={handleOverride}
              disabled={!overrideAction}
            >
              Apply Override
            </button>
            <button
              className="btn-ghost"
              onClick={() => setOverrideAction('')}
              disabled={!overrideAction}
            >
              Clear
            </button>
          </div>
        </GlowCard>

        <GlowCard title="Learning Progress" glowColor="primary">
          <div className="learning-progress">
            <div className="learning-stats">
              <div className="learning-stat">
                <span className="learning-stat-value">{mlpState.config.epochs}</span>
                <span className="learning-stat-label">Total Epochs</span>
              </div>
              <div className="learning-stat">
                <span className="learning-stat-value">{decisions.length}</span>
                <span className="learning-stat-label">Decisions</span>
              </div>
              <div className="learning-stat">
                <span className="learning-stat-value">{mlpState.config.hiddenLayers.length}</span>
                <span className="learning-stat-label">Hidden Layers</span>
              </div>
            </div>
            <div className="loss-chart-placeholder">
              <div className="loss-chart-bar-group">
                <div className="loss-chart-bar" style={{ height: `${Math.min(100, mlpState.config.loss * 100)}%` }} />
                <div className="loss-chart-bar-label">Loss</div>
              </div>
            </div>
          </div>
        </GlowCard>
      </div>

      <GlowCard title="Decision Timeline" glowColor="primary">
        <Timeline
          events={events}
          maxItems={30}
          emptyMessage="No MLP decisions yet. Enable the MLP engine to start receiving decisions."
        />
      </GlowCard>
    </PageWrapper>
  )
}
