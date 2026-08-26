import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { MainLayout } from '@/components/layout/MainLayout'
import { ErrorBoundary } from '@/components/common/ErrorBoundary'
import { UserAgreement } from '@/components/common/UserAgreement'
import { Dashboard } from '@/pages/Dashboard'
import { Performance } from '@/pages/Performance'
import { MlpPage } from '@/pages/MlpPage'
import { Binding } from '@/pages/Binding'
import { Aura } from '@/pages/Aura'
import { Settings } from '@/pages/Settings'
import { useSignalR } from '@/hooks/useSignalR'
import { useState, useEffect } from 'react'

const AGREEMENT_STORAGE_KEY = 'ztr_os_agreed_v3'

function AppContent() {
  useSignalR()

  const [showAgreement, setShowAgreement] = useState(false)

  useEffect(() => {
    const agreed = localStorage.getItem(AGREEMENT_STORAGE_KEY)
    if (!agreed) {
      setShowAgreement(true)
    }
  }, [])

  const handleAgree = () => {
    localStorage.setItem(AGREEMENT_STORAGE_KEY, new Date().toISOString())
    setShowAgreement(false)
  }

  const handleDisagree = () => {
    setShowAgreement(false)
  }

  return (
    <ErrorBoundary>
      <UserAgreement
        open={showAgreement}
        onAgree={handleAgree}
        onDisagree={handleDisagree}
      />
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