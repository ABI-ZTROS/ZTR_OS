import { useState, useEffect, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { ToggleSwitch } from '@/components/common/ToggleSwitch'
import { TopologyTree } from '@/components/common/TopologyTree'
import { useHardwareStore } from '@/store/useHardwareStore'
import { bindingApi } from '@/services/otherApi'
import type { ProcessInfo, CpuTopologyNode } from '@/types'
import './Binding.css'

export function Binding() {
  const hardware = useHardwareStore((s) => s.hardware)
  const isConnected = useHardwareStore((s) => s.isConnected)

  const [processes, setProcesses] = useState<ProcessInfo[]>([])
  const [topology, setTopology] = useState<CpuTopologyNode[]>([])
  const [selectedProcess, setSelectedProcess] = useState<ProcessInfo | null>(null)
  const [selectedCpuNode, setSelectedCpuNode] = useState<CpuTopologyNode | null>(null)
  const [autoBindGames, setAutoBindGames] = useState(true)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadData = useCallback(async () => {
    try {
      setIsLoading(true)
      const [procRes] = await Promise.allSettled([
        bindingApi.getProcesses(),
        bindingApi.getBindings(),
      ])

      if (procRes.status === 'fulfilled' && procRes.value.success) {
        const procs = procRes.value.data as unknown as ProcessInfo[]
        setProcesses(Array.isArray(procs) ? procs : [])
      }

      const cpuCount = hardware?.cpu.coreCount ?? 0
      if (cpuCount > 0) {
        const nodes: CpuTopologyNode[] = [{
          id: 0,
          type: 'package',
          name: 'CPU',
          children: Array.from({ length: cpuCount }, (_, i) => ({
            id: i + 1,
            type: 'core',
            name: `Core ${i}`,
            usage: hardware?.cpu.cores[i]?.usage ?? 0,
            temperature: hardware?.cpu.cores[i]?.temperature ?? 0,
          })),
        }]
        setTopology(nodes)
      }
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load bindings')
    } finally {
      setIsLoading(false)
    }
  }, [hardware])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleBindProcess = useCallback(async (processId: number, coreIds: number[]) => {
    try {
      await bindingApi.setBinding(processId, coreIds)
      setProcesses((prev) =>
        prev.map((p) =>
          p.id === processId ? { ...p, affinity: coreIds } : p
        )
      )
      setSelectedProcess(null)
      setSelectedCpuNode(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set binding')
    }
  }, [])

  const handleUnbindProcess = useCallback(async (processId: number) => {
    try {
      await bindingApi.removeBinding(processId)
      setProcesses((prev) =>
        prev.map((p) =>
          p.id === processId ? { ...p, affinity: [] } : p
        )
      )
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to remove binding')
    }
  }, [])

  const handleAutoBindToggle = useCallback(async (enabled: boolean) => {
    setAutoBindGames(enabled)
    try {
      await bindingApi.setAutoBindGames(enabled)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set auto-bind')
    }
  }, [])

  const handleGpuBind = useCallback(async (processId: number, gpuIndex: number) => {
    try {
      await bindingApi.setGpuAffinity(processId, gpuIndex)
      setProcesses((prev) =>
        prev.map((p) =>
          p.id === processId ? { ...p, gpuAffinity: [gpuIndex] } : p
        )
      )
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set GPU affinity')
    }
  }, [])

  const handleSelectProcess = useCallback((process: ProcessInfo) => {
    setSelectedProcess(process)
  }, [])

  const handleSelectCpuNode = useCallback((node: CpuTopologyNode) => {
    setSelectedCpuNode(node)
    if (selectedProcess && node.type === 'core') {
      handleBindProcess(selectedProcess.id, [node.id])
    }
  }, [selectedProcess, handleBindProcess])

  const gameProcesses = processes.filter((p) => p.isGame)
  const nonGameProcesses = processes.filter((p) => !p.isGame)

  return (
    <PageWrapper
      title="Process Binding"
      subtitle="Bind processes to specific CPU cores and manage affinity"
      actions={
        <div className="inline-group">
          <button className="btn-ghost" onClick={loadData} disabled={isLoading}>
            {isLoading ? 'Loading...' : 'Refresh'}
          </button>
        </div>
      }
    >
      {error && (
        <div className="binding-error">
          <span>⚠</span>
          <span>{error}</span>
        </div>
      )}

      <div className="grid-2">
        <GlowCard title="CPU Topology" glowColor="accent">
          {topology.length > 0 ? (
            <TopologyTree
              nodes={topology}
              selectedId={selectedCpuNode?.id}
              onSelect={handleSelectCpuNode}
              title="Physical CPU Layout"
            />
          ) : (
            <p className="placeholder-text">Connect to backend to view CPU topology.</p>
          )}
        </GlowCard>

        <GlowCard title="Binding Policy" glowColor="primary">
          <div className="card-section">
            <ToggleSwitch
              checked={autoBindGames}
              onChange={handleAutoBindToggle}
              label="Auto-bind game processes"
              description="Automatically bind detected game processes to dedicated cores"
              color="primary"
            />
            <div className="divider" />
            <div className="binding-summary">
              <div className="metric-row">
                <span className="metric-label">Total Processes</span>
                <span className="metric-value">{processes.length}</span>
              </div>
              <div className="metric-row">
                <span className="metric-label">Game Processes</span>
                <span className="metric-value">{gameProcesses.length}</span>
              </div>
              <div className="metric-row">
                <span className="metric-label">Bound Processes</span>
                <span className="metric-value">
                  {processes.filter((p) => p.affinity.length > 0).length}
                </span>
              </div>
              <div className="metric-row">
                <span className="metric-label">CPU Cores</span>
                <span className="metric-value">{hardware?.cpu.coreCount ?? 0}</span>
              </div>
            </div>
          </div>
        </GlowCard>
      </div>

      {selectedProcess && (
        <GlowCard title="Binding Action" glowColor="secondary">
          <div className="binding-action-panel">
            <div className="binding-action-info">
              <span className="binding-action-name">{selectedProcess.name}</span>
              <span className="binding-action-stats">
                CPU: {selectedProcess.cpuUsage.toFixed(1)}% | MEM: {selectedProcess.memoryUsage.toFixed(1)}%
              </span>
              {selectedProcess.affinity.length > 0 ? (
                <span className="chip chip--active">
                  Bound to cores: {selectedProcess.affinity.join(', ')}
                </span>
              ) : (
                <span className="chip">Unbound</span>
              )}
            </div>
            <div className="inline-group">
              <button
                className="btn-primary"
                onClick={() => {
                  if (selectedCpuNode && selectedCpuNode.type === 'core') {
                    handleBindProcess(selectedProcess.id, [selectedCpuNode.id])
                  }
                }}
                disabled={!selectedCpuNode || selectedCpuNode.type !== 'core'}
              >
                Bind to Selected Core
              </button>
              <button
                className="btn-ghost"
                onClick={() => handleUnbindProcess(selectedProcess.id)}
                disabled={selectedProcess.affinity.length === 0}
              >
                Unbind
              </button>
              <button
                className="btn-secondary"
                onClick={() => handleGpuBind(selectedProcess.id, 0)}
                disabled={!selectedProcess}
              >
                GPU Bind
              </button>
              <button className="btn-ghost" onClick={() => setSelectedProcess(null)}>
                Cancel
              </button>
            </div>
          </div>
        </GlowCard>
      )}

      <GlowCard title="Game Processes" glowColor="primary">
        {gameProcesses.length > 0 ? (
          <div className="process-list">
            {gameProcesses.map((process) => (
              <div
                key={process.id}
                className={`process-item ${selectedProcess?.id === process.id ? 'process-item--selected' : ''}`}
                onClick={() => handleSelectProcess(process)}
              >
                <div className="process-info">
                  <span className="process-name">{process.name}</span>
                  <span className="process-id">#{process.id}</span>
                </div>
                <div className="process-stats">
                  <div className="process-stat">
                    <span className="process-stat-label">CPU</span>
                    <div className="progress-bar">
                      <div
                        className={`progress-bar-fill ${process.cpuUsage > 80 ? 'progress-bar-fill--danger' : 'progress-bar-fill--primary'}`}
                        style={{ width: `${process.cpuUsage}%` }}
                      />
                    </div>
                    <span className="process-stat-value">{process.cpuUsage.toFixed(1)}%</span>
                  </div>
                  <div className="process-stat">
                    <span className="process-stat-label">MEM</span>
                    <div className="progress-bar">
                      <div
                        className="progress-bar-fill progress-bar-fill--accent"
                        style={{ width: `${process.memoryUsage}%` }}
                      />
                    </div>
                    <span className="process-stat-value">{process.memoryUsage.toFixed(1)}%</span>
                  </div>
                </div>
                <div className="process-affinity">
                  {process.affinity.length > 0 ? (
                    <span className="chip chip--active">Core(s): {process.affinity.join(', ')}</span>
                  ) : (
                    <span className="chip">Unbound</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="placeholder-text">
            {isConnected ? 'No game processes detected. Start a game to see it here.' : 'Waiting for backend connection...'}
          </p>
        )}
      </GlowCard>

      <GlowCard title="Other Processes" glowColor="accent">
        {nonGameProcesses.length > 0 ? (
          <div className="process-list">
            {nonGameProcesses.slice(0, 20).map((process) => (
              <div
                key={process.id}
                className={`process-item ${selectedProcess?.id === process.id ? 'process-item--selected' : ''}`}
                onClick={() => handleSelectProcess(process)}
              >
                <div className="process-info">
                  <span className="process-name">{process.name}</span>
                  <span className="process-id">#{process.id}</span>
                </div>
                <div className="process-stats">
                  <div className="process-stat">
                    <span className="process-stat-label">CPU</span>
                    <div className="progress-bar">
                      <div
                        className="progress-bar-fill progress-bar-fill--primary"
                        style={{ width: `${process.cpuUsage}%` }}
                      />
                    </div>
                    <span className="process-stat-value">{process.cpuUsage.toFixed(1)}%</span>
                  </div>
                  <div className="process-stat">
                    <span className="process-stat-label">MEM</span>
                    <div className="progress-bar">
                      <div
                        className="progress-bar-fill progress-bar-fill--accent"
                        style={{ width: `${process.memoryUsage}%` }}
                      />
                    </div>
                    <span className="process-stat-value">{process.memoryUsage.toFixed(1)}%</span>
                  </div>
                </div>
                <div className="process-affinity">
                  {process.affinity.length > 0 ? (
                    <span className="chip chip--active">Core(s): {process.affinity.join(', ')}</span>
                  ) : (
                    <span className="chip">Unbound</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="placeholder-text">
            {isConnected ? 'No process data.' : 'Waiting for backend connection...'}
          </p>
        )}
      </GlowCard>
    </PageWrapper>
  )
}
