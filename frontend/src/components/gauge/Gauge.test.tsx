import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Gauge } from './Gauge'

describe('Gauge', () => {
  it('renders with default props', () => {
    render(<Gauge value={50} />)
    const svg = document.querySelector('svg')
    expect(svg).toBeDefined()
  })

  it('displays the value', () => {
    render(<Gauge value={75} unit="°C" label="CPU Temp" />)
    const value = document.querySelector('.gauge-value')
    expect(value?.textContent).toBe('75')
    expect(screen.getByText('°C')).toBeDefined()
    expect(screen.getByText('CPU Temp')).toBeDefined()
  })

  it('clamps value to min/max range', () => {
    const { rerender } = render(<Gauge value={150} min={0} max={100} />)
    const value1 = document.querySelector('.gauge-value')
    expect(value1?.textContent).toBe('100')

    rerender(<Gauge value={-10} min={0} max={100} />)
    const value2 = document.querySelector('.gauge-value')
    expect(value2?.textContent).toBe('0')
  })

  it('renders with decimals', () => {
    render(<Gauge value={75.4} decimals={1} />)
    expect(screen.getByText('75.4')).toBeDefined()
  })

  it('applies normal status for values below warning threshold', () => {
    render(<Gauge value={50} warningThreshold={70} dangerThreshold={85} size="medium" />)
    const gauge = document.querySelector('.gauge')
    expect(gauge?.classList.contains('gauge--normal')).toBe(true)
  })

  it('applies warning status for values between warning and danger thresholds', () => {
    render(<Gauge value={75} warningThreshold={70} dangerThreshold={85} size="medium" />)
    const gauge = document.querySelector('.gauge')
    expect(gauge?.classList.contains('gauge--warning')).toBe(true)
  })

  it('applies danger status for values above danger threshold', () => {
    render(<Gauge value={90} warningThreshold={70} dangerThreshold={85} size="medium" />)
    const gauge = document.querySelector('.gauge')
    expect(gauge?.classList.contains('gauge--danger')).toBe(true)
  })

  it('renders large size variant', () => {
    render(<Gauge value={50} size="large" />)
    const gauge = document.querySelector('.gauge')
    expect(gauge?.classList.contains('gauge--large')).toBe(true)
    expect(gauge?.getAttribute('style')).toContain('240px')
  })

  it('renders small size variant', () => {
    render(<Gauge value={50} size="small" />)
    const gauge = document.querySelector('.gauge')
    expect(gauge?.classList.contains('gauge--small')).toBe(true)
    expect(gauge?.getAttribute('style')).toContain('100px')
  })

  it('renders needle element inside SVG', () => {
    render(<Gauge value={50} />)
    const needle = document.querySelector('.gauge-needle')
    expect(needle).toBeDefined()
  })

  it('renders tick marks', () => {
    render(<Gauge value={50} />)
    const ticks = document.querySelectorAll('.gauge-tick')
    expect(ticks.length).toBeGreaterThan(0)
  })

  it('renders number scale', () => {
    render(<Gauge value={50} min={0} max={100} />)
    const numbers = document.querySelectorAll('.gauge-number')
    expect(numbers.length).toBe(11)
  })

  it('renders neon glow filter', () => {
    render(<Gauge value={50} />)
    const filter = document.querySelector('.gauge-glow-filter')
    expect(filter).toBeDefined()
  })

  it('renders without unit and label', () => {
    render(<Gauge value={42} />)
    expect(screen.getByText('42')).toBeDefined()
    expect(screen.queryByText('°C')).toBeNull()
  })

  it('handles zero value', () => {
    render(<Gauge value={0} />)
    const value = document.querySelector('.gauge-value')
    expect(value?.textContent).toBe('0')
    const gauge = document.querySelector('.gauge')
    expect(gauge?.classList.contains('gauge--normal')).toBe(true)
  })

  it('handles max value', () => {
    render(<Gauge value={100} min={0} max={100} />)
    const value = document.querySelector('.gauge-value')
    expect(value?.textContent).toBe('100')
    const gauge = document.querySelector('.gauge')
    expect(gauge?.classList.contains('gauge--danger')).toBe(true)
  })

  it('needle position updates when value changes', () => {
    const { rerender } = render(<Gauge value={0} min={0} max={100} />)
    const needle1 = document.querySelector('.gauge-needle')
    const x1 = needle1?.getAttribute('x2')
    const y1 = needle1?.getAttribute('y2')

    rerender(<Gauge value={50} min={0} max={100} />)
    const needle2 = document.querySelector('.gauge-needle')
    const x2 = needle2?.getAttribute('x2')
    const y2 = needle2?.getAttribute('y2')

    expect(x1).not.toBe(x2)
    expect(y1).not.toBe(y2)
  })

  it('supports custom min/max ranges', () => {
    render(<Gauge value={150} min={100} max={300} decimals={0} />)
    expect(screen.getByText('150')).toBeDefined()
  })

  it('renders center display with glass morphism classes', () => {
    render(<Gauge value={50} />)
    const display = document.querySelector('.gauge-center-display')
    expect(display).toBeDefined()
    const value = document.querySelector('.gauge-value')
    expect(value?.classList.contains('gauge-value--normal')).toBe(true)
  })

  it('danger value triggers pulse animation', () => {
    render(<Gauge value={95} warningThreshold={70} dangerThreshold={85} />)
    const value = document.querySelector('.gauge-value')
    expect(value?.classList.contains('gauge-value--danger')).toBe(true)
  })
})
