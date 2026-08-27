import * as SignalR from '@microsoft/signalr'
import type { ConnectionStatus, HardwareState } from '@/types'
import { getHubUrl } from '@/config/apiConfig'

type EventCallback<T> = (data: T) => void

class SignalRService {
  private connection: SignalR.HubConnection | null = null
  private status: ConnectionStatus = 'disconnected'
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null
  private listeners: Map<string, Set<EventCallback<unknown>>> = new Map()

  getStatus(): ConnectionStatus {
    return this.status
  }

  async connect(): Promise<void> {
    if (this.connection?.state === SignalR.HubConnectionState.Connected) return

    this.updateStatus('connecting')

    const hubUrl = getHubUrl('hardware')

    this.connection = new SignalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: false,
        transport: SignalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000)
          return delay
        },
      })
      .build()

    this.connection.onclose(() => {
      this.updateStatus('disconnected')
      this.scheduleReconnect()
    })

    this.connection.onreconnecting(() => {
      this.updateStatus('reconnecting')
    })

    this.connection.onreconnected(() => {
      this.updateStatus('connected')
    })

    this.registerHandlers()

    try {
      await this.connection.start()
      this.updateStatus('connected')
    } catch {
      this.updateStatus('error')
      this.scheduleReconnect()
    }
  }

  async disconnect(): Promise<void> {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer)
      this.reconnectTimer = null
    }
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
    }
    this.updateStatus('disconnected')
  }

  on<T>(event: string, callback: EventCallback<T>): () => void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set())
    }
    this.listeners.get(event)!.add(callback as EventCallback<unknown>)

    return () => {
      this.listeners.get(event)?.delete(callback as EventCallback<unknown>)
    }
  }

  emit<T>(event: string, data: T): void {
    this.listeners.get(event)?.forEach((cb) => {
      try {
        cb(data)
      } catch {
        // swallow listener errors
      }
    })
  }

  private registerHandlers(): void {
    if (!this.connection) return

    // V6 FIXED: Subscribe to actual backend events.
    // Backend sends: "HardwareUpdate" (hardware hub), "SensorUpdate" (sensor hub),
    // "StateChange" (state hub). Previously subscribed to ghost events
    // (MlpStateUpdate/MlpDecision/SystemEvent) that were never sent → callbacks never fired.

    this.connection.on('HardwareUpdate', (data: HardwareState) => {
      this.emit('hardware:update', data)
    })

    this.connection.on('SensorUpdate', (data: HardwareState) => {
      this.emit('sensor:update', data)
      // Also forward as hardware update for compatibility
      this.emit('hardware:update', data)
    })

    this.connection.on('StateChange', (data: { type: string; payload: unknown }) => {
      this.emit(`state:${data.type}`, data.payload)
    })
  }

  private scheduleReconnect(): void {
    if (this.reconnectTimer) return
    this.reconnectTimer = setTimeout(async () => {
      this.reconnectTimer = null
      await this.connect()
    }, 3000) as unknown as ReturnType<typeof setTimeout>
  }

  private updateStatus(status: ConnectionStatus): void {
    this.status = status
    this.emit('connection:status', status)
  }
}

export const signalRService = new SignalRService()
