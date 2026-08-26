import type { ReactNode } from 'react'
import './GlowCard.css'

interface GlowCardProps {
  title?: string
  children: ReactNode
  glowColor?: 'primary' | 'secondary' | 'accent' | 'danger' | 'none'
  className?: string
}

export function GlowCard({ title, children, glowColor = 'primary', className = '' }: GlowCardProps) {
  return (
    <div className={`glow-card glow-card--${glowColor} ${className}`}>
      {title && <h3 className="glow-card-title">{title}</h3>}
      <div className="glow-card-body">{children}</div>
    </div>
  )
}