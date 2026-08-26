export const theme = {
  colors: {
    bgPrimary: '#0a0a0f',
    bgSecondary: '#0d0d14',
    bgCard: '#12121c',
    bgGlass: 'rgba(20, 20, 30, 0.6)',
    border: '#1f1f2e',
    borderGlow: '#2a2a3e',
    textPrimary: '#e0e0e8',
    textSecondary: '#8888a0',
    textMuted: '#555566',
    primary: '#00ffaa',
    primaryDim: '#00cc88',
    secondary: '#ff00aa',
    secondaryDim: '#cc0088',
    accent: '#00aaff',
    accentDim: '#0088cc',
    warning: '#ffaa00',
    danger: '#ff4444',
    success: '#00ff88',
  },
  shadows: {
    neonGreen: '0 0 8px rgba(0, 255, 170, 0.5), 0 0 20px rgba(0, 255, 170, 0.2)',
    neonPink: '0 0 8px rgba(255, 0, 170, 0.5), 0 0 20px rgba(255, 0, 170, 0.2)',
    neonBlue: '0 0 8px rgba(0, 170, 255, 0.5), 0 0 20px rgba(0, 170, 255, 0.2)',
    neonDanger: '0 0 8px rgba(255, 68, 68, 0.5), 0 0 20px rgba(255, 68, 68, 0.2)',
    card: '0 4px 20px rgba(0, 0, 0, 0.4)',
    glass: '0 8px 32px rgba(0, 0, 0, 0.3)',
  },
  radii: {
    sm: '4px',
    md: '8px',
    lg: '12px',
    xl: '16px',
    full: '9999px',
  },
  transitions: {
    fast: '150ms ease',
    normal: '250ms ease',
    slow: '400ms ease',
  },
} as const

export type Theme = typeof theme