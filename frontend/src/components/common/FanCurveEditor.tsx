import { useRef, useState, useCallback, useEffect } from 'react'
import type { FanCurvePoint } from '@/types'
import './FanCurveEditor.css'

interface FanCurveEditorProps {
  points: FanCurvePoint[]
  onChange: (points: FanCurvePoint[]) => void
  maxTemp?: number
  maxSpeed?: number
  label?: string
}

export function FanCurveEditor({
  points,
  onChange,
  maxTemp = 100,
  maxSpeed = 100,
  label,
}: FanCurveEditorProps) {
  const svgRef = useRef<SVGSVGElement>(null)
  const [draggingIndex, setDraggingIndex] = useState<number | null>(null)
  const [hoverIndex, setHoverIndex] = useState<number | null>(null)

  const width = 400
  const height = 200
  const padding = { top: 20, right: 20, bottom: 30, left: 40 }
  const innerW = width - padding.left - padding.right
  const innerH = height - padding.top - padding.bottom

  const tempToX = useCallback(
    (temp: number) => padding.left + (temp / maxTemp) * innerW,
    [maxTemp, innerW]
  )

  const speedToY = useCallback(
    (speed: number) => padding.top + innerH - (speed / maxSpeed) * innerH,
    [maxSpeed, innerH]
  )

  const xToTemp = useCallback(
    (x: number) => Math.max(0, Math.min(maxTemp, ((x - padding.left) / innerW) * maxTemp)),
    [maxTemp, innerW]
  )

  const yToSpeed = useCallback(
    (y: number) => Math.max(0, Math.min(maxSpeed, ((padding.top + innerH - y) / innerH) * maxSpeed)),
    [maxSpeed, innerH]
  )

  const getSVGPoint = useCallback((e: React.MouseEvent | MouseEvent) => {
    const svg = svgRef.current
    if (!svg) return { x: 0, y: 0 }
    const rect = svg.getBoundingClientRect()
    const scaleX = width / rect.width
    const scaleY = height / rect.height
    return {
      x: (e.clientX - rect.left) * scaleX,
      y: (e.clientY - rect.top) * scaleY,
    }
  }, [width, height])

  const handleMouseDown = useCallback(
    (index: number, e: React.MouseEvent) => {
      e.stopPropagation()
      setDraggingIndex(index)
    },
    []
  )

  const handleMouseMove = useCallback(
    (e: React.MouseEvent) => {
      if (draggingIndex === null) return
      const svgPoint = getSVGPoint(e)
      const newTemp = Math.round(xToTemp(svgPoint.x))
      const newSpeed = Math.round(yToSpeed(svgPoint.y))
      const updated = points.map((p, i) =>
        i === draggingIndex
          ? { temperature: newTemp, speed: newSpeed }
          : p
      )
      updated.sort((a, b) => a.temperature - b.temperature)
      const newIndex = updated.findIndex(
        (p) => p.temperature === newTemp && p.speed === newSpeed
      )
      onChange(updated)
      if (newIndex !== -1) setDraggingIndex(newIndex)
    },
    [draggingIndex, points, onChange, xToTemp, yToSpeed, getSVGPoint]
  )

  const handleMouseUp = useCallback(() => {
    setDraggingIndex(null)
  }, [])

  useEffect(() => {
    if (draggingIndex !== null) {
      const handleMove = (e: MouseEvent) => {
        const svgPoint = getSVGPoint(e)
        const newTemp = Math.round(xToTemp(svgPoint.x))
        const newSpeed = Math.round(yToSpeed(svgPoint.y))
        setHoverIndex(draggingIndex)
        setDraggingIndex((prev) => {
          if (prev === null) return null
          const updated = points.map((p, i) =>
            i === prev ? { temperature: newTemp, speed: newSpeed } : p
          )
          updated.sort((a, b) => a.temperature - b.temperature)
          onChange(updated)
          const newIndex = updated.findIndex(
            (p) => p.temperature === newTemp && p.speed === newSpeed
          )
          return newIndex !== -1 ? newIndex : null
        })
      }
      const handleUp = () => setDraggingIndex(null)
      window.addEventListener('mousemove', handleMove)
      window.addEventListener('mouseup', handleUp)
      return () => {
        window.removeEventListener('mousemove', handleMove)
        window.removeEventListener('mouseup', handleUp)
      }
    }
  }, [draggingIndex, points, onChange, getSVGPoint, xToTemp, yToSpeed])

  const pathData = points
    .map((p, i) => `${i === 0 ? 'M' : 'L'} ${tempToX(p.temperature)} ${speedToY(p.speed)}`)
    .join(' ')

  const areaPath =
    points.length > 0
      ? `${pathData} L ${tempToX(points[points.length - 1].temperature)} ${padding.top + innerH} L ${tempToX(points[0].temperature)} ${padding.top + innerH} Z`
      : ''

  const tempTicks = Array.from({ length: 6 }, (_, i) => Math.round((maxTemp / 5) * i))
  const speedTicks = Array.from({ length: 6 }, (_, i) => Math.round((maxSpeed / 5) * i))

  return (
    <div className="fan-curve-editor">
      {label && <div className="fan-curve-label">{label}</div>}
      <svg
        ref={svgRef}
        viewBox={`0 0 ${width} ${height}`}
        className="fan-curve-svg"
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
      >
        {tempTicks.map((t) => (
          <g key={`temp-${t}`}>
            <line
              x1={tempToX(t)}
              y1={padding.top}
              x2={tempToX(t)}
              y2={padding.top + innerH}
              className="fan-curve-grid"
            />
            <text
              x={tempToX(t)}
              y={height - 8}
              className="fan-curve-axis-label"
              textAnchor="middle"
            >
              {t}°C
            </text>
          </g>
        ))}
        {speedTicks.map((s) => (
          <g key={`speed-${s}`}>
            <line
              x1={padding.left}
              y1={speedToY(s)}
              x2={padding.left + innerW}
              y2={speedToY(s)}
              className="fan-curve-grid"
            />
            <text
              x={padding.left - 6}
              y={speedToY(s) + 3}
              className="fan-curve-axis-label"
              textAnchor="end"
            >
              {s}%
            </text>
          </g>
        ))}
        {areaPath && (
          <path d={areaPath} className="fan-curve-area" />
        )}
        {pathData && (
          <path d={pathData} className="fan-curve-line" />
        )}
        {points.map((p, i) => (
          <g key={`point-${i}`}>
            <circle
              cx={tempToX(p.temperature)}
              cy={speedToY(p.speed)}
              r={draggingIndex === i || hoverIndex === i ? 8 : 6}
              className={`fan-curve-point ${draggingIndex === i ? 'fan-curve-point--dragging' : ''}`}
              onMouseDown={(e) => handleMouseDown(i, e)}
              onMouseEnter={() => setHoverIndex(i)}
              onMouseLeave={() => setHoverIndex(null)}
            />
            {(draggingIndex === i || hoverIndex === i) && (
              <g>
                <rect
                  x={tempToX(p.temperature) - 30}
                  y={speedToY(p.speed) - 30}
                  width={60}
                  height={18}
                  className="fan-curve-tooltip"
                  rx={4}
                />
                <text
                  x={tempToX(p.temperature)}
                  y={speedToY(p.speed) - 18}
                  className="fan-curve-tooltip-text"
                  textAnchor="middle"
                >
                  {p.temperature}°C / {p.speed}%
                </text>
              </g>
            )}
          </g>
        ))}
      </svg>
      <div className="fan-curve-axis-label fan-curve-axis-label--x">Temperature</div>
      <div className="fan-curve-axis-label fan-curve-axis-label--y">Speed</div>
    </div>
  )
}
