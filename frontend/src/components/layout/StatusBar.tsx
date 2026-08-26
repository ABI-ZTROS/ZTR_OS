import { useHardwareStore } from '@/store/useHardwareStore'
import './StatusBar.css'

export function StatusBar() {
  const connectionStatus = useHardwareStore((s) => s.connectionStatus)

  const statusMap: Record<string, { label: string; color: string }> = {
    connecting: { label: '连接中', color: 'var(--warning)' },
    connected: { label: '在线', color: 'var(--success)' },
    disconnected: { label: '已断开', color: 'var(--text-muted)' },
    reconnecting: { label: '重连中', color: 'var(--warning)' },
    error: { label: '错误', color: 'var(--danger)' },
  }

  const status = statusMap[connectionStatus] || statusMap.disconnected

  return (
    <header className="status-bar">
      <div className="status-left">
        <span className="status-title">ZTR_OS 控制台</span>
      </div>
      <div className="status-right">
        <div className="status-indicator">
          <span
            className="status-dot"
            style={{ backgroundColor: status.color, boxShadow: `0 0 8px ${status.color}` }}
          />
          <span className="status-label">{status.label}</span>
        </div>
      </div>
    </header>
  )
}