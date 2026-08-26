import type { TimelineEvent } from '@/types'
import './Timeline.css'

interface TimelineProps {
  events: TimelineEvent[]
  maxItems?: number
  emptyMessage?: string
}

const typeColors: Record<TimelineEvent['type'], string> = {
  decision: 'var(--primary)',
  config: 'var(--accent)',
  training: 'var(--secondary)',
  system: 'var(--warning)',
}

export function Timeline({ events, maxItems = 20, emptyMessage = 'No events yet' }: TimelineProps) {
  const displayEvents = events.slice(0, maxItems)

  if (displayEvents.length === 0) {
    return <div className="timeline-empty">{emptyMessage}</div>
  }

  return (
    <div className="timeline">
      {displayEvents.map((event, index) => (
        <div
          key={event.id}
          className={`timeline-event ${index === 0 ? 'timeline-event--latest' : ''}`}
          style={{ '--event-color': typeColors[event.type] } as React.CSSProperties}
        >
          <div className="timeline-node" />
          <div className="timeline-content">
            <div className="timeline-header">
              <span className="timeline-title">{event.title}</span>
              <span className="timeline-time">
                {new Date(event.timestamp).toLocaleTimeString()}
              </span>
            </div>
            {event.description && (
              <div className="timeline-description">{event.description}</div>
            )}
            {event.metadata && (
              <div className="timeline-metadata">
                {Object.entries(event.metadata).map(([key, value]) => (
                  <span key={key} className="timeline-meta-item">
                    <span className="timeline-meta-key">{key}:</span>
                    <span className="timeline-meta-value">
                      {typeof value === 'number' ? value.toFixed(2) : String(value)}
                    </span>
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}
