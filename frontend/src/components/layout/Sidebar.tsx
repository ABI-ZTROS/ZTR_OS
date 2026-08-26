import { NavLink } from 'react-router-dom'
import './Sidebar.css'

const navItems = [
  { to: '/', label: 'Dashboard', icon: '◉' },
  { to: '/performance', label: 'Performance', icon: '⚡' },
  { to: '/mlp', label: 'MLP', icon: '◎' },
  { to: '/binding', label: 'Binding', icon: '⬡' },
  { to: '/aura', label: 'Aura', icon: '✦' },
  { to: '/settings', label: 'Settings', icon: '⚙' },
]

export function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <div className="logo">
          <span className="logo-icon">Z</span>
          <span className="logo-text">ZTR_OS</span>
        </div>
      </div>
      <nav className="sidebar-nav">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              `nav-item${isActive ? ' nav-item--active' : ''}`
            }
            end={item.to === '/'}
          >
            <span className="nav-icon">{item.icon}</span>
            <span className="nav-label">{item.label}</span>
          </NavLink>
        ))}
      </nav>
      <div className="sidebar-footer">
        <span className="sidebar-version">v0.1.0</span>
      </div>
    </aside>
  )
}