import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { API_BASE, type ItemChange } from './api'

export type HubStatus = 'connecting' | 'live' | 'reconnecting' | 'offline'

/** Subscribes to the API's SignalR hub and calls `onChange` for every create, update or delete. */
export function useCatalogHub(onChange: (change: ItemChange) => void): HubStatus {
  const [status, setStatus] = useState<HubStatus>('connecting')
  const handler = useRef(onChange)
  handler.current = onChange

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/catalog`)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('itemChanged', (change: ItemChange) => handler.current(change))
    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(() => setStatus('live'))
    connection.onclose(() => setStatus('offline'))

    let cancelled = false
    let retry: ReturnType<typeof setTimeout>
    const start = async () => {
      try {
        await connection.start()
        if (!cancelled) setStatus('live')
      } catch {
        if (cancelled) return
        setStatus('offline')
        retry = setTimeout(start, 5000) // API not up yet: keep trying
      }
    }
    // Deferred one tick so React StrictMode's mount/unmount/mount in dev doesn't start then abort a connection.
    retry = setTimeout(start, 0)

    return () => {
      cancelled = true
      clearTimeout(retry)
      if (connection.state !== HubConnectionState.Disconnected) connection.stop()
    }
  }, [])

  return status
}
