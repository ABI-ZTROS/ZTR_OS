import type { ReactNode } from 'react'
import './PageWrapper.css'

interface PageWrapperProps {
  title: string
  subtitle?: string
  children: ReactNode
  actions?: ReactNode
}

export function PageWrapper({ title, subtitle, children, actions }: PageWrapperProps) {
  return (
    <div className="page-wrapper">
      <div className="page-header">
        <div className="page-title-group">
          <h1 className="page-title">{title}</h1>
          {subtitle && <p className="page-subtitle">{subtitle}</p>}
        </div>
        {actions && <div className="page-actions">{actions}</div>}
      </div>
      <div className="page-body">{children}</div>
    </div>
  )
}