import { useHardwareStore } from '@/store/useHardwareStore'
import './StatusBar.css'

export function StatusBar() {
  const connectionStatus = useHardwareStore((s) => s.connectionStatus)

  const statusMap: Record<string, { label: string; color: string }> = {
    connecting: { label: 'Connecting', color: 'var(--warning)' },
    connected: { label: 'Online', color: 'var(--success)' },
    disconnected: { label: 'Disconnected', color: 'var(--text-muted)' },
    reconnecting: { label: 'Reconnecting', color: 'var(--warning)' },
    error: { label: 'Error', color: 'var(--danger)' },
  }

  const status = statusMap[connectionStatus] || statusMap.disconnected

  return (
    <header className="status-bar">
      <div className="status-left">
        <span className="status-title">ZTR_OS Control Panel</span>
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