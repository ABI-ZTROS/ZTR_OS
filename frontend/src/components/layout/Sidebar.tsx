import { NavLink } from 'react-router-dom'
import './Sidebar.css'

const navItems = [
  { to: '/', label: '仪表盘', icon: '◉' },
  { to: '/performance', label: '性能', icon: '⚡' },
  { to: '/mlp', label: '机器学习', icon: '◎' },
  { to: '/binding', label: '进程绑定', icon: '⬡' },
  { to: '/aura', label: '灯效', icon: '✦' },
  { to: '/settings', label: '设置', icon: '⚙' },
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