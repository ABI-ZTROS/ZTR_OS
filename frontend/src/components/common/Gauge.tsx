import type { GaugeConfig } from '@/types'
import './Gauge.css'

interface GaugeProps {
  config: GaugeConfig
  size?: number
}

export function Gauge({ config, size = 120 }: GaugeProps) {
  const { label, max, unit, color } = config
  const numericValue = typeof config.value === 'number' && Number.isFinite(config.value) ? config.value : 0
  const percentage = Math.min(100, Math.max(0, (numericValue / max) * 100))
  const radius = (size - 12) / 2
  const circumference = 2 * Math.PI * radius
  const offset = circumference * (1 - percentage / 100)
  const strokeWidth = 8

  const gradientId = `gauge-grad-${label.replace(/\s+/g, '-')}`

  return (
    <div className="gauge-container" style={{ width: size, height: size + 28 }}>
      <svg width={size} height={size} className="gauge-svg">
        <defs>
          <linearGradient id={gradientId} x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor={color} stopOpacity="0.6" />
            <stop offset="100%" stopColor={color} stopOpacity="1" />
          </linearGradient>
        </defs>
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="var(--border)"
          strokeWidth={strokeWidth}
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke={`url(#${gradientId})`}
          strokeWidth={strokeWidth}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          transform={`rotate(-90 ${size / 2} ${size / 2})`}
          style={{ transition: 'stroke-dashoffset 0.3s ease' }}
        />
        <text
          x={size / 2}
          y={size / 2 - 4}
          className="gauge-value"
          textAnchor="middle"
          fill="var(--text-primary)"
        >
          {numericValue.toFixed(unit === '°C' ? 0 : 1)}
        </text>
        <text
          x={size / 2}
          y={size / 2 + 14}
          className="gauge-unit"
          textAnchor="middle"
          fill="var(--text-muted)"
        >
          {unit}
        </text>
      </svg>
      <div className="gauge-label">{label}</div>
    </div>
  )
}
