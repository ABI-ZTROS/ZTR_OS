import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { MainLayout } from '@/components/layout/MainLayout'
import { ErrorBoundary } from '@/components/common/ErrorBoundary'
import { GameOverlay } from '@/components/common/GameOverlay'
import { Dashboard } from '@/pages/Dashboard'
import { Performance } from '@/pages/Performance'
import { GpuTuning } from '@/pages/GpuTuning'
import { Screen } from '@/pages/Screen'
import { MlpPage } from '@/pages/MlpPage'
import { Binding } from '@/pages/Binding'
import { Aura } from '@/pages/Aura'
import { Automation } from '@/pages/Automation'
import { Updates } from '@/pages/Updates'
import { Settings } from '@/pages/Settings'
import { useSignalR } from '@/hooks/useSignalR'

function AppContent() {
  useSignalR()

  return (
    <ErrorBoundary>
      <GameOverlay />
      <Routes>
        <Route element={<MainLayout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/performance" element={<Performance />} />
          <Route path="/gpu-tuning" element={<GpuTuning />} />
          <Route path="/screen" element={<Screen />} />
          <Route path="/mlp" element={<MlpPage />} />
          <Route path="/binding" element={<Binding />} />
          <Route path="/aura" element={<Aura />} />
          <Route path="/automation" element={<Automation />} />
          <Route path="/updates" element={<Updates />} />
          <Route path="/settings" element={<Settings />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </ErrorBoundary>
  )
}

function App() {
  return (
    <BrowserRouter>
      <AppContent />
    </BrowserRouter>
  )
}

export default App