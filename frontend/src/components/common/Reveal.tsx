import { type ReactNode, type JSX } from 'react'
import { useInView } from '@/hooks/useInView'
import { usePrefersReducedMotion } from '@/hooks/usePrefersReducedMotion'

type RevealDirection = 'up' | 'down' | 'left' | 'right' | 'fade' | 'scale'

interface RevealProps {
  children: ReactNode
  direction?: RevealDirection
  delay?: number
  duration?: number
  once?: boolean
  threshold?: number
  className?: string
  style?: React.CSSProperties
  as?: keyof JSX.IntrinsicElements
}

const directionTransform: Record<RevealDirection, string> = {
  up: 'translateY(16px)',
  down: 'translateY(-16px)',
  left: 'translateX(20px)',
  right: 'translateX(-20px)',
  fade: 'none',
  scale: 'scale(0.96)',
}

export function Reveal({
  children,
  direction = 'up',
  delay = 0,
  duration,
  once = true,
  threshold = 0.15,
  className,
  style,
  as = 'div',
}: RevealProps) {
  const reduced = usePrefersReducedMotion()
  const { ref, inView } = useInView<HTMLDivElement>({ once, threshold })

  const Tag = as as 'div'

  if (reduced) {
    return (
      <Tag className={className} style={style}>
        {children}
      </Tag>
    )
  }

  const transitionDuration = duration != null ? `${duration}ms` : '400ms'
  const transitionDelay = delay > 0 ? `${delay}ms` : '0ms'

  const revealStyle: React.CSSProperties = {
    opacity: inView ? 1 : 0,
    transition: `opacity ${transitionDuration} cubic-bezier(0.4, 0, 0.2, 1) ${transitionDelay}, transform ${transitionDuration} cubic-bezier(0.4, 0, 0.2, 1) ${transitionDelay}`,
    willChange: inView ? 'auto' : 'opacity, transform',
  }
  if (!inView) {
    revealStyle.transform = directionTransform[direction]
  }

  return (
    <Tag
      ref={ref}
      className={className}
      style={{
        ...revealStyle,
        ...style,
      }}
    >
      {children}
    </Tag>
  )
}
