import { useEffect, useRef } from 'react'
import { usePrefersReducedMotion } from '@/hooks/usePrefersReducedMotion'

interface ParticleFieldProps {
  density?: number
  color?: string
  connect?: boolean
  connectDistance?: number
  speed?: number
  radiusRange?: [number, number]
  maxOpacity?: number
  pauseOnHidden?: boolean
  className?: string
  style?: React.CSSProperties
}

interface Particle {
  x: number
  y: number
  vx: number
  vy: number
  r: number
  baseAlpha: number
}

export function ParticleField({
  density = 1,
  color = 'var(--primary-color, #00aaff)',
  connect = true,
  connectDistance = 120,
  speed = 0.25,
  radiusRange = [0.6, 1.8],
  maxOpacity = 0.5,
  pauseOnHidden = true,
  className,
  style,
}: ParticleFieldProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const reduced = usePrefersReducedMotion()

  useEffect(() => {
    if (reduced) return
    const canvas = canvasRef.current
    if (!canvas) return

    const ctx = canvas.getContext('2d', { alpha: true })
    if (!ctx) return

    let raf = 0
    let particles: Particle[] = []
    let width = 0
    let height = 0
    let dpr = Math.min(window.devicePixelRatio || 1, 2)
    let running = true

    const resolveColor = (raw: string): string => {
      try {
        canvas.style.color = raw
        const resolved = getComputedStyle(canvas).color
        return resolved || raw
      } catch {
        return raw
      }
    }
    let resolvedColor = resolveColor(color)

    const computeCount = () => {
      const area = width * height
      return Math.min(120, Math.max(12, Math.floor((area / 25000) * density)))
    }

    const initParticles = () => {
      const count = computeCount()
      particles = []
      for (let i = 0; i < count; i++) {
        const [rMin, rMax] = radiusRange
        particles.push({
          x: Math.random() * width,
          y: Math.random() * height,
          vx: (Math.random() - 0.5) * speed * 2,
          vy: (Math.random() - 0.5) * speed * 2,
          r: rMin + Math.random() * (rMax - rMin),
          baseAlpha: 0.15 + Math.random() * (maxOpacity - 0.15),
        })
      }
    }

    const resize = () => {
      const rect = canvas.getBoundingClientRect()
      width = rect.width
      height = rect.height
      dpr = Math.min(window.devicePixelRatio || 1, 2)
      canvas.width = Math.floor(width * dpr)
      canvas.height = Math.floor(height * dpr)
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0)
      initParticles()
    }

    const draw = () => {
      ctx.clearRect(0, 0, width, height)

      for (const p of particles) {
        p.x += p.vx
        p.y += p.vy

        if (p.x < -10) p.x = width + 10
        else if (p.x > width + 10) p.x = -10
        if (p.y < -10) p.y = height + 10
        else if (p.y > height + 10) p.y = -10

        ctx.beginPath()
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2)
        ctx.fillStyle = resolvedColor
        ctx.globalAlpha = p.baseAlpha
        ctx.fill()
      }

      if (connect && particles.length <= 80) {
        ctx.globalAlpha = 1
        ctx.strokeStyle = resolvedColor
        ctx.lineWidth = 0.6
        const distSq = connectDistance * connectDistance
        for (let i = 0; i < particles.length; i++) {
          const a = particles[i]
          for (let j = i + 1; j < particles.length; j++) {
            const b = particles[j]
            const dx = a.x - b.x
            const dy = a.y - b.y
            const d2 = dx * dx + dy * dy
            if (d2 < distSq) {
              const alpha = (1 - Math.sqrt(d2) / connectDistance) * 0.22
              ctx.globalAlpha = alpha
              ctx.beginPath()
              ctx.moveTo(a.x, a.y)
              ctx.lineTo(b.x, b.y)
              ctx.stroke()
            }
          }
        }
      }

      ctx.globalAlpha = 1
      raf = requestAnimationFrame(draw)
    }

    const onVisibility = () => {
      const visible = !document.hidden
      if (visible && !running) {
        running = true
        raf = requestAnimationFrame(draw)
      } else if (!visible && running) {
        running = false
        cancelAnimationFrame(raf)
      }
    }

    resize()
    raf = requestAnimationFrame(draw)

    const ro = new ResizeObserver(() => resize())
    ro.observe(canvas)
    if (pauseOnHidden) {
      document.addEventListener('visibilitychange', onVisibility)
    }

    return () => {
      cancelAnimationFrame(raf)
      ro.disconnect()
      if (pauseOnHidden) {
        document.removeEventListener('visibilitychange', onVisibility)
      }
    }
  }, [reduced, density, color, connect, connectDistance, speed, radiusRange, maxOpacity, pauseOnHidden])

  return (
    <canvas
      ref={canvasRef}
      aria-hidden
      className={className}
      style={{
        position: 'absolute',
        inset: 0,
        width: '100%',
        height: '100%',
        pointerEvents: 'none',
        ...style,
      }}
    />
  )
}
