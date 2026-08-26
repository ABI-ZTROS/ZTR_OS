import { useState, useCallback } from 'react'
import { PageWrapper } from '@/components/layout/PageWrapper'
import { GlowCard } from '@/components/common/GlowCard'
import { api } from '@/services/api'
import type { ApiResponse } from '@/types'
import './Updates.css'

interface UpdateItem {
  component: string
  currentVersion: string
  latestVersion: string
  downloadUrl: string
  type: 'bios' | 'driver' | 'firmware' | 'software'
  releaseNotes?: string
}

interface CheckUpdatesResponse {
  updates: UpdateItem[]
  lastChecked: string | null
}

const TYPE_ICONS: Record<UpdateItem['type'], string> = {
  bios: '🔧',
  driver: '⚙️',
  firmware: '🔌',
  software: '💾',
}

const TYPE_LABELS: Record<UpdateItem['type'], string> = {
  bios: 'BIOS',
  driver: 'Driver',
  firmware: 'Firmware',
  software: 'Software',
}

export function Updates() {
  const [updates, setUpdates] = useState<UpdateItem[]>([])
  const [lastChecked, setLastChecked] = useState<string | null>(null)
  const [isChecking, setIsChecking] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hasChecked, setHasChecked] = useState(false)

  const handleCheck = useCallback(async () => {
    try {
      setIsChecking(true)
      setError(null)

      const res: ApiResponse<CheckUpdatesResponse> = await api.get<CheckUpdatesResponse>(
        '/api/updates/check'
      )

      if (res.success && res.data) {
        const data = res.data
        setUpdates(data.updates ?? [])
        setLastChecked(data.lastChecked ?? new Date().toISOString())
      } else {
        setError(res.message ?? 'Failed to check for updates')
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to check for updates')
    } finally {
      setIsChecking(false)
      setHasChecked(true)
    }
  }, [])

  const formatTimestamp = (iso: string | null): string => {
    if (!iso) return 'Never'
    try {
      const d = new Date(iso)
      return d.toLocaleString()
    } catch {
      return iso
    }
  }

  return (
    <PageWrapper
      title="Updates"
      subtitle="Check for BIOS, driver, and firmware updates"
      actions={
        <button
          className="updates-check-btn"
          onClick={handleCheck}
          disabled={isChecking}
        >
          {isChecking ? (
            <>
              <span className="updates-spinner" />
              Checking...
            </>
          ) : (
            <>
              <span className="updates-check-icon">⟳</span>
              Check for Updates
            </>
          )}
        </button>
      }
    >
      {error && (
        <div className="updates-error">
          <span className="updates-error-icon">⚠</span>
          <span>{error}</span>
          <button
            className="updates-retry-btn"
            onClick={handleCheck}
            disabled={isChecking}
          >
            Retry
          </button>
        </div>
      )}

      <GlowCard title="Update Status" glowColor="accent">
        <div className="updates-status-row">
          <div className="updates-status-item">
            <span className="updates-status-label">Last Checked</span>
            <span className="updates-status-value">
              {formatTimestamp(lastChecked)}
            </span>
          </div>
          <div className="updates-status-item">
            <span className="updates-status-label">Updates Available</span>
            <span className="updates-status-value updates-count">
              {updates.length}
            </span>
          </div>
          <div className="updates-status-item">
            <span className="updates-status-label">Status</span>
            <span className="updates-status-value">
              {isChecking ? (
                <span className="updates-status-checking">Checking...</span>
              ) : hasChecked ? (
                updates.length > 0 ? (
                  <span className="updates-status-found">Updates Found</span>
                ) : (
                  <span className="updates-status-none">Up to date</span>
                )
              ) : (
                <span className="updates-status-idle">Idle</span>
              )}
            </span>
          </div>
        </div>
      </GlowCard>

      {isChecking && (
        <GlowCard title="Scanning" glowColor="accent">
          <div className="updates-loading">
            <div className="updates-loading-spinner" />
            <p>Checking for available updates...</p>
            <p className="updates-loading-sub">
              This may take a moment as we query the latest versions for your
              system components.
            </p>
          </div>
        </GlowCard>
      )}

      {!isChecking && hasChecked && updates.length === 0 && !error && (
        <GlowCard title="No Updates Found" glowColor="primary">
          <div className="updates-empty">
            <div className="updates-empty-icon">✓</div>
            <p className="updates-empty-title">Your system is up to date</p>
            <p className="updates-empty-sub">
              All components are running the latest available versions. Check
              back later or click &quot;Check for Updates&quot; to scan again.
            </p>
          </div>
        </GlowCard>
      )}

      {!isChecking && updates.length > 0 && (
        <GlowCard
          title={`Available Updates (${updates.length})`}
          glowColor="accent"
        >
          <div className="updates-list">
            {updates.map((update, index) => (
              <div key={`${update.component}-${index}`} className="update-item">
                <div className="update-item-header">
                  <div className="update-item-type">
                    <span className="update-item-type-icon">
                      {TYPE_ICONS[update.type]}
                    </span>
                    <span className="update-item-type-label">
                      {TYPE_LABELS[update.type]}
                    </span>
                  </div>
                  <span className="update-item-component">{update.component}</span>
                </div>

                <div className="update-item-versions">
                  <div className="update-item-version">
                    <span className="update-item-version-label">Current</span>
                    <span className="update-item-version-value update-item-version-current">
                      {update.currentVersion}
                    </span>
                  </div>
                  <span className="update-item-arrow">→</span>
                  <div className="update-item-version">
                    <span className="update-item-version-label">Latest</span>
                    <span className="update-item-version-value update-item-version-latest">
                      {update.latestVersion}
                    </span>
                  </div>
                </div>

                {update.releaseNotes && (
                  <p className="update-item-notes">{update.releaseNotes}</p>
                )}

                <div className="update-item-actions">
                  <a
                    href={update.downloadUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="update-item-download"
                  >
                    <span>Download</span>
                    <span className="update-item-download-icon">↗</span>
                  </a>
                </div>
              </div>
            ))}
          </div>
        </GlowCard>
      )}
    </PageWrapper>
  )
}
