import { useCallback } from 'react'
import './SliderControl.css'

interface SliderControlProps {
  label: string
  value: number
  min: number
  max: number
  step?: number
  unit?: string
  onChange: (value: number) => void
  color?: 'primary' | 'secondary' | 'accent'
  formatValue?: (v: number) => string
}

export function SliderControl({
  label,
  value,
  min,
  max,
  step = 1,
  unit = '',
  onChange,
  color = 'primary',
  formatValue,
}: SliderControlProps) {
  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange(Number(e.target.value))
    },
    [onChange]
  )

  const displayValue = formatValue ? formatValue(value) : `${value}${unit}`
  const percentage = ((value - min) / (max - min)) * 100

  return (
    <div className={`slider-control slider-control--${color}`}>
      <div className="slider-header">
        <span className="slider-label">{label}</span>
        <span className="slider-value">{displayValue}</span>
      </div>
      <div className="slider-track-wrapper">
        <div
          className="slider-track-fill"
          style={{ width: `${percentage}%` }}
        />
        <input
          type="range"
          className="slider-input"
          min={min}
          max={max}
          step={step}
          value={value}
          onChange={handleChange}
        />
        <div className="slider-ticks">
          <span>{min}{unit}</span>
          <span>{max}{unit}</span>
        </div>
      </div>
    </div>
  )
}
