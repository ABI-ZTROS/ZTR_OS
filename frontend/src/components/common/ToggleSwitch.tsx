import './ToggleSwitch.css'

interface ToggleSwitchProps {
  checked: boolean
  onChange: (checked: boolean) => void
  label?: string
  description?: string
  color?: 'primary' | 'secondary' | 'accent'
  disabled?: boolean
}

export function ToggleSwitch({
  checked,
  onChange,
  label,
  description,
  color = 'primary',
  disabled = false,
}: ToggleSwitchProps) {
  return (
    <label className={`toggle-row ${disabled ? 'toggle-row--disabled' : ''}`}>
      {(label || description) && (
        <div className="toggle-text">
          {label && <span className="toggle-label">{label}</span>}
          {description && <span className="toggle-desc">{description}</span>}
        </div>
      )}
      <span className="toggle-switch-wrapper">
        <input
          type="checkbox"
          className="toggle-input"
          checked={checked}
          disabled={disabled}
          onChange={(e) => onChange(e.target.checked)}
        />
        <span className={`toggle-slider toggle-slider--${color}`} />
      </span>
    </label>
  )
}
