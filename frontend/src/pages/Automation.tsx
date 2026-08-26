import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ToggleSwitch } from '@/components/common/ToggleSwitch'
import { Reveal } from '@/components/common/Reveal'
import { automationApi, type AutomationRule, type AutomationStatus } from '@/services/automationApi'
import './Automation.css'

const TRIGGER_OPTIONS = [
  { value: 'ac', label: 'AC电源' },
  { value: 'battery', label: '电池' },
]

const PERFORMANCE_MODES = [
  { value: '', label: '不改变' },
  { value: 'silent', label: '静音' },
  { value: 'balanced', label: '平衡' },
  { value: 'turbo', label: '涡轮增压' },
  { value: 'fullspeed', label: '全速' },
  { value: 'manual', label: '手动' },
]

const GPU_MODES = [
  { value: '', label: '不改变' },
  { value: 'eco', label: 'Eco' },
  { value: 'standard', label: 'Standard' },
  { value: 'ultimate', label: 'Ultimate' },
  { value: 'optimized', label: 'Optimized' },
]

const REFRESH_RATE_OPTIONS = [
  { value: 0, label: '不改变' },
  { value: 60, label: '60 Hz' },
  { value: 120, label: '120 Hz' },
  { value: 144, label: '144 Hz' },
  { value: 240, label: '240 Hz' },
]

const KEYBOARD_TIMEOUT_OPTIONS = [
  { value: 0, label: '不改变' },
  { value: 30, label: '30秒' },
  { value: 60, label: '1分钟' },
  { value: 120, label: '2分钟' },
  { value: 300, label: '5分钟' },
]

const CHARGE_LIMIT_OPTIONS = [
  { value: 0, label: '不改变' },
  { value: 60, label: '60%' },
  { value: 80, label: '80%' },
  { value: 100, label: '100%' },
]

interface RuleFormState {
  trigger: string
  name: string
  performanceMode: string
  gpuMode: string
  refreshRate: number
  keyboardTimeoutSeconds: number
  chargeLimit: number
  optimizeGpu: boolean
}

const initialFormState: RuleFormState = {
  trigger: 'ac',
  name: '',
  performanceMode: '',
  gpuMode: '',
  refreshRate: 0,
  keyboardTimeoutSeconds: 0,
  chargeLimit: 0,
  optimizeGpu: false,
}

export function Automation() {
  const [status, setStatus] = useState<AutomationStatus>({
    isRunning: false,
    isEnabled: false,
    rules: [],
  })

  const [formState, setFormState] = useState<RuleFormState>(initialFormState)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadStatus = useCallback(async () => {
    try {
      setIsRefreshing(true)
      const res = await automationApi.getStatus()
      if (res.success && res.data) {
        setStatus(res.data)
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : '加载自动化状态失败')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }, [])

  useEffect(() => {
    loadStatus()
  }, [loadStatus])

  const handleStart = useCallback(async () => {
    try {
      const res = await automationApi.start()
      if (res.success) {
        setStatus((prev) => ({ ...prev, isRunning: true }))
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '启动自动化服务失败')
    }
  }, [])

  const handleStop = useCallback(async () => {
    try {
      const res = await automationApi.stop()
      if (res.success) {
        setStatus((prev) => ({ ...prev, isRunning: false }))
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '停止自动化服务失败')
    }
  }, [])

  const handleEnabledChange = useCallback(async (enabled: boolean) => {
    try {
      const res = await automationApi.updateConfig(enabled)
      if (res.success) {
        setStatus((prev) => ({ ...prev, isEnabled: enabled }))
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '更新启用状态失败')
    }
  }, [])

  const handleRemoveRule = useCallback(async (ruleName: string) => {
    try {
      const res = await automationApi.removeRule(ruleName)
      if (res.success) {
        setStatus((prev) => ({
          ...prev,
          rules: prev.rules.filter((r) => r.name !== ruleName),
        }))
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '删除规则失败')
    }
  }, [])

  const handleApplyNow = useCallback(async (trigger: string) => {
    try {
      await automationApi.applyNow(trigger)
    } catch (e) {
      setError(e instanceof Error ? e.message : '应用规则失败')
    }
  }, [])

  const handleAddRule = useCallback(async () => {
    if (!formState.name.trim()) {
      setError('请输入规则名称')
      return
    }
    try {
      setIsSubmitting(true)
      const ruleData: Omit<AutomationRule, 'name'> & { name?: string } = {
        trigger: formState.trigger,
        name: formState.name.trim(),
        performanceMode: formState.performanceMode || undefined,
        gpuMode: formState.gpuMode || undefined,
        refreshRate: formState.refreshRate || undefined,
        keyboardTimeoutSeconds: formState.keyboardTimeoutSeconds || undefined,
        chargeLimit: formState.chargeLimit || undefined,
        optimizeGpu: formState.optimizeGpu,
      }
      const res = await automationApi.addRule(ruleData)
      if (res.success) {
        setFormState(initialFormState)
        await loadStatus()
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '添加规则失败')
    } finally {
      setIsSubmitting(false)
    }
  }, [formState, loadStatus])

  const updateForm = useCallback(<K extends keyof RuleFormState>(key: K, value: RuleFormState[K]) => {
    setFormState((prev) => ({ ...prev, [key]: value }))
  }, [])

  if (isLoading) {
    return (
      <PageWrapper title="自动化规则" subtitle="自动性能切换和优化">
        <div className="automation-loading">加载自动化状态中...</div>
      </PageWrapper>
    )
  }

  return (
    <PageWrapper
      title="自动化规则"
      subtitle="基于触发器的自动性能优化规则管理"
      actions={
        <button
          className="btn-ghost"
          onClick={() => loadStatus()}
          disabled={isRefreshing}
        >
          {isRefreshing ? '刷新中...' : '刷新'}
        </button>
      }
    >
      {error && (
        <div className="automation-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <Reveal direction="fade" duration={400}>
        <GlowCard title="服务状态" glowColor="primary">
          <div className="automation-status-row">
            <div className="automation-status-item">
              <div className="automation-status-label">运行状态</div>
              <div className="automation-status-value">
                <span
                  className={`automation-status-dot ${status.isRunning ? 'automation-status-dot--running' : 'automation-status-dot--stopped'}`}
                />
                <span className={status.isRunning ? 'automation-status-running' : 'automation-status-stopped'}>
                  {status.isRunning ? '运行中' : '已停止'}
                </span>
              </div>
            </div>

            <div className="automation-status-item">
              <div className="automation-status-label">自动化</div>
              <ToggleSwitch
                checked={status.isEnabled}
                onChange={handleEnabledChange}
                label=""
                description=""
                color="primary"
              />
            </div>

            <div className="automation-status-item automation-status-actions">
              <button
                className="btn-primary"
                onClick={handleStart}
                disabled={status.isRunning}
              >
                启动
              </button>
              <button
                className="btn-ghost"
                onClick={handleStop}
                disabled={!status.isRunning}
              >
                停止
              </button>
            </div>
          </div>
        </GlowCard>
      </Reveal>

      <Reveal direction="up" delay={80}>
        <GlowCard title="添加新规则" glowColor="accent">
          <div className="automation-form">
            <div className="automation-form-row">
              <div className="automation-form-col">
                <label className="form-label">规则名称</label>
                <input
                  type="text"
                  className="form-input"
                  value={formState.name}
                  onChange={(e) => updateForm('name', e.target.value)}
                  placeholder="输入规则名称..."
                />
              </div>
              <div className="automation-form-col">
                <label className="form-label">触发器</label>
                <select
                  className="form-select"
                  value={formState.trigger}
                  onChange={(e) => updateForm('trigger', e.target.value)}
                >
                  {TRIGGER_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="automation-form-row">
              <div className="automation-form-col">
                <label className="form-label">性能模式</label>
                <select
                  className="form-select"
                  value={formState.performanceMode}
                  onChange={(e) => updateForm('performanceMode', e.target.value)}
                >
                  {PERFORMANCE_MODES.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="automation-form-col">
                <label className="form-label">GPU模式</label>
                <select
                  className="form-select"
                  value={formState.gpuMode}
                  onChange={(e) => updateForm('gpuMode', e.target.value)}
                >
                  {GPU_MODES.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="automation-form-row">
              <div className="automation-form-col">
                <label className="form-label">刷新率</label>
                <select
                  className="form-select"
                  value={formState.refreshRate}
                  onChange={(e) => updateForm('refreshRate', Number(e.target.value))}
                >
                  {REFRESH_RATE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="automation-form-col">
                <label className="form-label">键盘超时</label>
                <select
                  className="form-select"
                  value={formState.keyboardTimeoutSeconds}
                  onChange={(e) => updateForm('keyboardTimeoutSeconds', Number(e.target.value))}
                >
                  {KEYBOARD_TIMEOUT_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="automation-form-row">
              <div className="automation-form-col">
                <label className="form-label">充电限制</label>
                <select
                  className="form-select"
                  value={formState.chargeLimit}
                  onChange={(e) => updateForm('chargeLimit', Number(e.target.value))}
                >
                  {CHARGE_LIMIT_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="automation-form-col automation-form-checkbox">
                <ToggleSwitch
                  checked={formState.optimizeGpu}
                  onChange={(v) => updateForm('optimizeGpu', v)}
                  label="优化GPU"
                  description="自动优化GPU设置"
                  color="accent"
                />
              </div>
            </div>

            <button
              className="btn-primary automation-add-btn"
              onClick={handleAddRule}
              disabled={isSubmitting}
            >
              {isSubmitting ? '添加中...' : '添加规则'}
            </button>
          </div>
        </GlowCard>
      </Reveal>

      <Reveal direction="up" delay={120}>
        <GlowCard title={`规则列表 (${status.rules.length})`} glowColor="primary">
          {status.rules.length > 0 ? (
            <div className="automation-rules-list">
              {status.rules.map((rule) => (
                <div key={rule.name} className="automation-rule-item">
                  <div className="automation-rule-header">
                    <div className="automation-rule-title">
                      <span className="automation-rule-name">{rule.name}</span>
                      <span className="automation-rule-trigger">
                        {rule.trigger === 'ac' ? 'AC电源' : '电池'}
                      </span>
                    </div>
                    <div className="automation-rule-actions">
                      <button
                        className="btn-ghost automation-apply-btn"
                        onClick={() => handleApplyNow(rule.trigger)}
                      >
                        立即应用
                      </button>
                      <button
                        className="btn-ghost automation-remove-btn"
                        onClick={() => rule.name && handleRemoveRule(rule.name)}
                      >
                        移除
                      </button>
                    </div>
                  </div>
                  <div className="automation-rule-details">
                    {rule.performanceMode && (
                      <span className="automation-rule-tag">性能: {rule.performanceMode}</span>
                    )}
                    {rule.gpuMode && (
                      <span className="automation-rule-tag">GPU: {rule.gpuMode}</span>
                    )}
                    {rule.refreshRate && rule.refreshRate > 0 && (
                      <span className="automation-rule-tag">刷新率: {rule.refreshRate}Hz</span>
                    )}
                    {rule.keyboardTimeoutSeconds && rule.keyboardTimeoutSeconds > 0 && (
                      <span className="automation-rule-tag">键盘超时: {rule.keyboardTimeoutSeconds}s</span>
                    )}
                    {rule.chargeLimit && rule.chargeLimit > 0 && (
                      <span className="automation-rule-tag">充电限制: {rule.chargeLimit}%</span>
                    )}
                    {rule.optimizeGpu && (
                      <span className="automation-rule-tag automation-rule-tag--accent">GPU优化</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="placeholder-text">暂无配置的规则，添加一条规则开始自动化。</p>
          )}
        </GlowCard>
      </Reveal>
    </PageWrapper>
  )
}
