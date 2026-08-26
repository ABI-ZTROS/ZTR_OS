import { useEffect } from 'react'
import { signalRService } from '@/services/signalR'
import { useHardwareStore } from '@/store/useHardwareStore'
import { useMlpStore } from '@/store/useMlpStore'

export function useSignalR() {
  useEffect(() => {
    useHardwareStore.getState().subscribe()
    useMlpStore.getState().subscribe()
    signalRService.connect().catch(() => {})

    return () => {
      useHardwareStore.getState().unsubscribe()
      useMlpStore.getState().unsubscribe()
    }
  }, [])
}