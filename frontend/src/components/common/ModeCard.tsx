import type { ReactNode } from 'react'
import './ModeCard.css'

interface ModeCardProps {
  title: string
  description?: string
  icon?: ReactNode
  selected: boolean
  onClick: () => void
  glowColor?: 'primary' | 'secondary' | 'accent'
  badge?: string
}

export function ModeCard({
  title,
  description,
  icon,
  selected,
  onClick,
  glowColor = 'primary',
  badge,
}: ModeCardProps) {
  return (
    <button
      type="button"
      className={`mode-card mode-card--${glowColor} ${selected ? 'mode-card--selected' : ''}`}
      onClick={onClick}
    >
      {badge && <span className="mode-card-badge">{badge}</span>}
      {icon && <div className="mode-card-icon">{icon}</div>}
      <div className="mode-card-title">{title}</div>
      {description && <div className="mode-card-desc">{description}</div>}
      <span className="mode-card-check">
        {selected ? '●' : '○'}
      </span>
    </button>
  )
}
