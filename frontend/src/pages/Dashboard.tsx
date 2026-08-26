import { PageWrapper } from '@/components/layout/PageWrapper'
import { Gauge } from '@/components/gauge/Gauge'
import { NeonGrid } from '@/components/common/NeonGrid'
import { useHardwareStore } from '@/store/useHardwareStore'
import './Dashboard.css'

export function Dashboard() {
  const hardware = useHardwareStore((s) => s.hardware)
  const isConnected = useHardwareStore((s) => s.isConnected)

  const cpu = hardware?.cpu
  const gpu = hardware?.gpu
  const battery = hardware?.battery
  const fans = hardware?.fans ?? []

  const cpuFanSpeed = fans.find((f) => f.name.toLowerCase().includes('cpu'))?.speed ?? 0
  const gpuFanSpeed = fans.find((f) => f.name.toLowerCase().includes('gpu'))?.speed ?? 0

  return (
    <>
      <NeonGrid />
      <PageWrapper title="Dashboard" subtitle="System overview and real-time metrics">
        {!isConnected && (
          <div className="dashboard-banner">
            <span className="banner-icon">⚠</span>
            <span>Waiting for backend connection. Start the ZTR.Api to see live data.</span>
          </div>
        )}

        <div className="dashboard-gauges">
          <div className="gauge-row gauge-row--large">
            <Gauge
              min={0}
              max={100}
              value={cpu?.temperature ?? 0}
              unit="°C"
              label="CPU Temp"
              warningThreshold={70}
              dangerThreshold={85}
              size="large"
              decimals={0}
            />
            <Gauge
              min={0}
              max={100}
              value={gpu?.temperature ?? 0}
              unit="°C"
              label="GPU Temp"
              warningThreshold={70}
              dangerThreshold={85}
              size="large"
              decimals={0}
            />
          </div>

          <div className="gauge-row gauge-row--medium">
            <Gauge
              min={0}
              max={300}
              value={cpu?.powerDraw ?? 0}
              unit="W"
              label="CPU Power"
              warningThreshold={70}
              dangerThreshold={90}
              size="medium"
              decimals={1}
            />
            <Gauge
              min={0}
              max={500}
              value={gpu?.powerDraw ?? 0}
              unit="W"
              label="GPU Power"
              warningThreshold={70}
              dangerThreshold={90}
              size="medium"
              decimals={1}
            />
            <Gauge
              min={0}
              max={100}
              value={cpu?.usage ?? 0}
              unit="%"
              label="CPU Usage"
              warningThreshold={70}
              dangerThreshold={85}
              size="medium"
              decimals={1}
            />
            <Gauge
              min={0}
              max={100}
              value={gpu?.usage ?? 0}
              unit="%"
              label="GPU Usage"
              warningThreshold={70}
              dangerThreshold={85}
              size="medium"
              decimals={1}
            />
          </div>

          <div className="gauge-row gauge-row--small">
            <Gauge
              min={0}
              max={100}
              value={cpuFanSpeed}
              unit="%"
              label="Fan CPU"
              warningThreshold={80}
              dangerThreshold={95}
              size="small"
              decimals={0}
            />
            <Gauge
              min={0}
              max={100}
              value={gpuFanSpeed}
              unit="%"
              label="Fan GPU"
              warningThreshold={80}
              dangerThreshold={95}
              size="small"
              decimals={0}
            />
            <Gauge
              min={0}
              max={100}
              value={battery?.percentage ?? 0}
              unit="%"
              label="Battery"
              warningThreshold={30}
              dangerThreshold={15}
              size="small"
              decimals={0}
            />
            <Gauge
              min={0}
              max={5000}
              value={gpu?.clockSpeed ?? 0}
              unit="MHz"
              label="GPU Clock"
              warningThreshold={70}
              dangerThreshold={90}
              size="small"
              decimals={0}
            />
          </div>
        </div>

        <div className="dashboard-status">
          <div className="status-item">
            <span
              className="status-dot-indicator"
              style={{ background: isConnected ? 'var(--success)' : 'var(--text-muted)' }}
            />
            <span>Backend SignalR: {isConnected ? 'Connected' : 'Disconnected'}</span>
          </div>
          <div className="status-item">
            <span className="status-dot-indicator" style={{ background: 'var(--primary)' }} />
            <span>Fans: {fans.length} detected</span>
          </div>
          <div className="status-item">
            <span className="status-dot-indicator" style={{ background: 'var(--accent)' }} />
            <span>CPU Cores: {cpu?.coreCount ?? 0} / Threads: {cpu?.threadCount ?? 0}</span>
          </div>
          <div className="status-item">
            <span className="status-dot-indicator" style={{ background: 'var(--secondary)' }} />
            <span>Battery: {battery?.status ?? 'Unknown'}</span>
          </div>
        </div>
      </PageWrapper>
    </>
  )
}
