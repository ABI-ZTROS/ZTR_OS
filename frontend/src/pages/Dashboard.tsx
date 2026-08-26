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
      <PageWrapper title="仪表盘" subtitle="系统概览与实时指标">
        {!isConnected && (
          <div className="dashboard-banner">
            <span className="banner-icon">⚠</span>
            <span>等待后端连接，请启动 ZTR.Api 以查看实时数据</span>
          </div>
        )}

        <div className="dashboard-gauges">
          <div className="gauge-row gauge-row--large">
            <Gauge
              min={0}
              max={100}
              value={cpu?.temperature ?? 0}
              unit="°C"
              label="CPU温度"
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
              label="GPU温度"
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
              label="CPU功耗"
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
              label="GPU功耗"
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
              label="CPU使用率"
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
              label="GPU使用率"
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
              label="CPU风扇"
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
              label="GPU风扇"
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
              label="电池"
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
              label="GPU时钟"
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
            <span>后端SignalR: {isConnected ? '已连接' : '已断开'}</span>
          </div>
          <div className="status-item">
            <span className="status-dot-indicator" style={{ background: 'var(--primary)' }} />
            <span>风扇: 检测到{fans.length}个</span>
          </div>
          <div className="status-item">
            <span className="status-dot-indicator" style={{ background: 'var(--accent)' }} />
            <span>CPU核心: {cpu?.coreCount ?? 0} / 线程: {cpu?.threadCount ?? 0}</span>
          </div>
          <div className="status-item">
            <span className="status-dot-indicator" style={{ background: 'var(--secondary)' }} />
            <span>电池: {battery?.status ?? '未知'}</span>
          </div>
        </div>
      </PageWrapper>
    </>
  )
}
