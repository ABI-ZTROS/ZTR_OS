import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { StatusBar } from './StatusBar'
import { ParticleField } from '@/components/common/ParticleField'
import './MainLayout.css'

export function MainLayout() {
  return (
    <div className="main-layout">
      <ParticleField
        density={0.35}
        color="var(--primary, #00aaff)"
        connect
        connectDistance={140}
        speed={0.18}
        radiusRange={[0.5, 1.4]}
        maxOpacity={0.32}
        style={{ opacity: 0.6 }}
      />
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
