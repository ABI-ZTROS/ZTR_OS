import { useState, useRef, useEffect } from 'react'
import './ColorPicker.css'

interface ColorPickerProps {
  color: string
  onChange: (color: string) => void
  label?: string
}

const NEON_PALETTE = [
  '#00ffaa',
  '#ff00aa',
  '#00aaff',
  '#ffaa00',
  '#ff4444',
  '#aa00ff',
  '#00ff88',
  '#ffff00',
  '#ff8800',
  '#00ffff',
  '#ff0088',
  '#88ff00',
]

export function ColorPicker({ color, onChange, label }: ColorPickerProps) {
  const [isOpen, setIsOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  return (
    <div className="color-picker" ref={ref}>
      {label && <span className="color-picker-label">{label}</span>}
      <button
        type="button"
        className="color-picker-trigger"
        style={{ backgroundColor: color }}
        onClick={() => setIsOpen(!isOpen)}
        aria-label={`Pick color ${color}`}
      />
      {isOpen && (
        <div className="color-picker-panel">
          <div className="color-picker-custom">
            <span>Custom:</span>
            <input
              type="color"
              value={color}
              onChange={(e) => onChange(e.target.value)}
              className="color-picker-native"
            />
            <input
              type="text"
              value={color}
              onChange={(e) => onChange(e.target.value)}
              className="color-picker-hex"
            />
          </div>
          <div className="color-picker-preset-label">Neon Palette</div>
          <div className="color-picker-grid">
            {NEON_PALETTE.map((c) => (
              <button
                key={c}
                type="button"
                className={`color-picker-swatch ${color.toLowerCase() === c.toLowerCase() ? 'color-picker-swatch--selected' : ''}`}
                style={{ backgroundColor: c }}
                onClick={() => {
                  onChange(c)
                  setIsOpen(false)
                }}
                aria-label={`Select color ${c}`}
              />
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
