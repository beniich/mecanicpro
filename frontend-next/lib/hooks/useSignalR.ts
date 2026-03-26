import { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/lib/store'

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'

export function useNotificationHub() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [notifications, setNotifications] = useState<any[]>([])
  const token = useAuthStore(state => state.token)

  useEffect(() => {
    if (!token) return

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/notifications`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build()

    setConnection(newConnection)

    return () => {
      newConnection.stop()
    }
  }, [token])

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('🔗 SignalR NotificationHub Connected')
          connection.on('ReceiveNotification', (notification) => {
            console.log('🔔 Notification reçue:', notification)
            setNotifications(prev => [notification, ...prev])
          })
        })
        .catch(e => console.log('❌ SignalR Connection Error: ', e))
    }
  }, [connection])

  return { connection, notifications }
}

export function useChatHub(vehicleId: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [messages, setMessages] = useState<any[]>([])
  const token = useAuthStore(state => state.token)

  useEffect(() => {
    if (!token || !vehicleId) return

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build()

    setConnection(newConnection)

    return () => {
      newConnection.stop()
    }
  }, [token, vehicleId])

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('🔗 SignalR ChatHub Connected')
          connection.invoke('JoinVehicleRoom', vehicleId)
          
          connection.on('OnMessage', (message) => {
            console.log('💬 Message reçu:', message)
            setMessages(prev => [...prev, message])
          })
        })
        .catch(e => console.log('❌ SignalR Chat Connection Error: ', e))
    }
  }, [connection, vehicleId])

  const sendMessage = async (content: string) => {
    if (connection?.state === signalR.HubConnectionState.Connected) {
      await connection.invoke('SendMessage', vehicleId, content)
    }
  }

  return { connection, messages, sendMessage }
}
