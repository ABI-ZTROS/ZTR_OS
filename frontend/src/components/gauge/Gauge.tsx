import { useMemo } from 'react'
import './Gauge.css'

export type GaugeSize = 'large' | 'medium' | 'small'

export interface GaugeProps {
  min?: number
  max?: number
  value: number
  unit?: string
  label?: string
  warningThreshold?: number
  dangerThreshold?: number
  size?: GaugeSize
  decimals?: number
}

const SIZE_MAP: Record<GaugeSize, number> = {
  large: 240,
  medium: 160,
  small: 100,
}

const START_ANGLE = -135
const END_ANGLE = 135
const ANGLE_RANGE = END_ANGLE - START_ANGLE

function polar(cx: number, cy: number, r: number, angleDeg: number) {
  const rad = (angleDeg * Math.PI) / 180
  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) }
}

function arcPath(cx: number, cy: number, r: number, startAngle: number, endAngle: number) {
  const start = polar(cx, cy, r, startAngle)
  const end = polar(cx, cy, r, endAngle)
  const largeArc = Math.abs(endAngle - startAngle) > 180 ? 1 : 0
  const sweep = endAngle > startAngle ? 1 : 0
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArc} ${sweep} ${end.x} ${end.y}`
}

function getStatusColor(percent: number, warning: number, danger: number) {
  if (percent >= danger) return 'danger'
  if (percent >= warning) return 'warning'
  return 'normal'
}

export function Gauge({
  min = 0,
  max = 100,
  value,
  unit = '',
  label = '',
  warningThreshold = 70,
  dangerThreshold = 85,
  size = 'medium',
  decimals = 0,
}: GaugeProps) {
  const sizePx = SIZE_MAP[size]
  const cx = sizePx / 2
  const cy = sizePx / 2
  const outerR = sizePx * 0.42
  const innerR = sizePx * 0.32
  const tickR1 = sizePx * 0.44
  const tickR2 = sizePx * 0.38
  const labelR = sizePx * 0.28
  const needleLen = sizePx * 0.36
  const strokeWidth = sizePx * 0.025

  const clampedValue = Math.max(min, Math.min(max, value))
  const percent = ((clampedValue - min) / (max - min)) * 100
  const needleAngle = START_ANGLE + (percent / 100) * ANGLE_RANGE

  const status = getStatusColor(percent, warningThreshold, dangerThreshold)

  const tickMarks = useMemo(() => {
    const majorTicks = 10
    const minorTicks = 50
    const marks: { x1: number; y1: number; x2: number; y2: number; major: boolean; angle: number }[] = []

    for (let i = 0; i <= minorTicks; i++) {
      const t = i / minorTicks
      const angle = START_ANGLE + t * ANGLE_RANGE
      const isMajor = i % (minorTicks / majorTicks) === 0
      const r1 = isMajor ? tickR1 + sizePx * 0.02 : tickR1
      const r2 = tickR2
      const p1 = polar(cx, cy, r1, angle)
      const p2 = polar(cx, cy, r2, angle)
      marks.push({ x1: p1.x, y1: p1.y, x2: p2.x, y2: p2.y, major: isMajor, angle })
    }
    return marks
  }, [cx, cy, tickR1, tickR2, sizePx])

  const numberLabels = useMemo(() => {
    const count = 10
    const labels: { x: number; y: number; text: string }[] = []
    for (let i = 0; i <= count; i++) {
      const t = i / count
      const angle = START_ANGLE + t * ANGLE_RANGE
      const p = polar(cx, cy, labelR, angle)
      const val = Math.round(min + t * (max - min))
      labels.push({ x: p.x, y: p.y, text: String(val) })
    }
    return labels
  }, [cx, cy, labelR, min, max])

  const normalEnd = START_ANGLE + (warningThreshold / 100) * ANGLE_RANGE
  const warningEnd = START_ANGLE + (dangerThreshold / 100) * ANGLE_RANGE

  const needleEnd = polar(cx, cy, needleLen, needleAngle)

  const displayValue = decimals > 0 ? clampedValue.toFixed(decimals) : Math.round(clampedValue).toString()

  const glowId = `gauge-glow-${size}-${Math.abs(Math.round(min))}-${Math.abs(Math.round(max))}`

  return (
    <div className={`gauge gauge--${size} gauge--${status}`} style={{ width: sizePx, height: sizePx }}>
      <svg
        viewBox={`0 0 ${sizePx} ${sizePx}`}
        width={sizePx}
        height={sizePx}
        className="gauge-svg"
      >
        <defs>
          <filter id={glowId} className="gauge-glow-filter">
            <feGaussianBlur stdDeviation="2.5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <linearGradient id={`${glowId}-grad`} x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="var(--success)" />
            <stop offset={`${warningThreshold}%`} stopColor="var(--warning)" />
            <stop offset={`${dangerThreshold}%`} stopColor="var(--danger)" />
          </linearGradient>
        </defs>

        <path
          d={arcPath(cx, cy, outerR, START_ANGLE, END_ANGLE)}
          className="gauge-track"
          fill="none"
          stroke="var(--border)"
          strokeWidth={strokeWidth}
          strokeLinecap="round"
        />

        <path
          d={arcPath(cx, cy, outerR, START_ANGLE, normalEnd)}
          className="gauge-zone gauge-zone--normal"
          fill="none"
          stroke="var(--success)"
          strokeWidth={strokeWidth}
          strokeLinecap="round"
          opacity={0.4}
        />
        <path
          d={arcPath(cx, cy, outerR, normalEnd, warningEnd)}
          className="gauge-zone gauge-zone--warning"
          fill="none"
          stroke="var(--warning)"
          strokeWidth={strokeWidth}
          strokeLinecap="round"
          opacity={0.4}
        />
        <path
          d={arcPath(cx, cy, outerR, warningEnd, END_ANGLE)}
          className="gauge-zone gauge-zone--danger"
          fill="none"
          stroke="var(--danger)"
          strokeWidth={strokeWidth}
          strokeLinecap="round"
          opacity={0.4}
        />

        <g filter={`url(#${glowId})`} className="gauge-ticks">
          {tickMarks.map((tick, i) => (
            <line
              key={i}
              x1={tick.x1}
              y1={tick.y1}
              x2={tick.x2}
              y2={tick.y2}
              className={`gauge-tick ${tick.major ? 'gauge-tick--major' : 'gauge-tick--minor'}`}
            />
          ))}
        </g>

        <g className="gauge-numbers">
          {numberLabels.map((lbl, i) => (
            <text
              key={i}
              x={lbl.x}
              y={lbl.y}
              className="gauge-number"
              textAnchor="middle"
              dominantBaseline="middle"
            >
              {lbl.text}
            </text>
          ))}
        </g>

        <g filter={`url(#${glowId})`} className="gauge-needle-group">
          <line
            x1={cx}
            y1={cy}
            x2={needleEnd.x}
            y2={needleEnd.y}
            className="gauge-needle"
            strokeLinecap="round"
          />
          <circle cx={cx} cy={cy} r={sizePx * 0.04} className="gauge-center-cap" />
        </g>

        <circle cx={cx} cy={cy} r={innerR} className="gauge-center-bg" />
        <circle cx={cx} cy={cy} r={innerR - 2} className="gauge-center-inner" />
      </svg>

      <div className="gauge-center-display">
        <span className={`gauge-value gauge-value--${status}`}>{displayValue}</span>
        {unit && <span className="gauge-unit">{unit}</span>}
      </div>
      {label && <div className="gauge-label">{label}</div>}
    </div>
  )
}
