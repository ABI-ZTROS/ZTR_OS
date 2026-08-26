import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { MainLayout } from '@/components/layout/MainLayout'
import { Dashboard } from '@/pages/Dashboard'
import { Performance } from '@/pages/Performance'
import { MlpPage } from '@/pages/MlpPage'
import { Binding } from '@/pages/Binding'
import { Aura } from '@/pages/Aura'
import { Settings } from '@/pages/Settings'
import { useSignalR } from '@/hooks/useSignalR'

function AppContent() {
  useSignalR()

  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route path="/" element={<Dashboard />} />
        <Route path="/performance" element={<Performance />} />
        <Route path="/mlp" element={<MlpPage />} />
        <Route path="/binding" element={<Binding />} />
        <Route path="/aura" element={<Aura />} />
        <Route path="/settings" element={<Settings />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
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