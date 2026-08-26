import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { StatusBar } from './StatusBar'
import './MainLayout.css'

export function MainLayout() {
  return (
    <div className="main-layout">
      <Sidebar />
      <div className="main-content">
        <StatusBar />
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}