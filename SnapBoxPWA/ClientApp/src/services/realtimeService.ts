import axios from 'axios'
import { useSettingsStore } from '../stores/settingsStore'

export interface RealtimeSession {
  client_secret: string
  expires_at: number
  model: string
  modalities: string[]
  voice: string
}

export interface RealtimeServiceOptions {
  model?: string
  voice?: 'alloy' | 'echo' | 'shimmer'
  instructions?: string
  tools?: any[] // Function definitions for function calling
}

export class RealtimeService {
  private peerConnection: RTCPeerConnection | null = null
  protected dataChannel: RTCDataChannel | null = null
  private audioElement: HTMLAudioElement | null = null
  private sessionData: RealtimeSession | null = null
  private eventHandlers: Map<string, Function[]> = new Map()

  constructor() {
    this.audioElement = new Audio()
    this.audioElement.autoplay = true
  }

  /**
   * Get ephemeral token from server
   */
  async createSession(options?: RealtimeServiceOptions): Promise<RealtimeSession> {
    const settingsStore = useSettingsStore()
    const config = {
      baseURL: settingsStore.apiBaseUrl,
      headers: {
        'Authorization': settingsStore.getAuthHeader()
      }
    }

    const requestData = {
      model: options?.model,
      voice: options?.voice,
      instructions: options?.instructions,
      tools: options?.tools
    }

    console.log('createSession: Sending request:', requestData)

    const response = await axios.post<RealtimeSession>(
      '/Realtime/session',
      requestData,
      config
    )

    console.log('createSession: Received response:', response.data)
    this.sessionData = response.data
    return response.data
  }

  /**
   * Connect to OpenAI Realtime API using WebRTC
   */
  async connect(session?: RealtimeSession): Promise<void> {
    const sessionToUse = session || this.sessionData

    if (!sessionToUse) {
      throw new Error('No session data available. Call createSession first.')
    }

    // Create peer connection
    this.peerConnection = new RTCPeerConnection()

    // Set up audio element to play remote audio
    this.peerConnection.ontrack = (event) => {
      if (this.audioElement) {
        this.audioElement.srcObject = event.streams[0]
      }
    }

    // Add local audio track (microphone)
    const mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true })
    mediaStream.getTracks().forEach((track) => {
      this.peerConnection?.addTrack(track, mediaStream)
    })

    // Set up data channel for sending/receiving events
    this.dataChannel = this.peerConnection.createDataChannel('oai-events')
    this.dataChannel.addEventListener('message', (event) => {
      this.handleServerEvent(JSON.parse(event.data))
    })

    // Create and set local description
    const offer = await this.peerConnection.createOffer()
    await this.peerConnection.setLocalDescription(offer)

    // Send offer to OpenAI and get answer
    const baseUrl = 'https://api.openai.com/v1/realtime'
    const model = sessionToUse.model
    const response = await fetch(`${baseUrl}?model=${model}`, {
      method: 'POST',
      body: offer.sdp,
      headers: {
        Authorization: `Bearer ${sessionToUse.client_secret}`,
        'Content-Type': 'application/sdp'
      }
    })

    if (!response.ok) {
      throw new Error(`Failed to connect: ${response.statusText}`)
    }

    const answer = {
      type: 'answer' as RTCSdpType,
      sdp: await response.text()
    }

    await this.peerConnection.setRemoteDescription(answer)
  }

  /**
   * Send an event to the server
   */
  protected sendEvent(event: any): void {
    if (!this.dataChannel || this.dataChannel.readyState !== 'open') {
      throw new Error('Data channel is not open')
    }

    this.dataChannel.send(JSON.stringify(event))
  }

  /**
   * Send a text message to the assistant
   */
  sendMessage(text: string): void {
    if (!this.dataChannel || this.dataChannel.readyState !== 'open') {
      throw new Error('Data channel is not open')
    }

    const event = {
      type: 'conversation.item.create',
      item: {
        type: 'message',
        role: 'user',
        content: [
          {
            type: 'input_text',
            text: text
          }
        ]
      }
    }

    this.sendEvent(event)

    // Trigger response
    this.sendEvent({ type: 'response.create' })
  }

  /**
   * Interrupt the current response
   */
  interrupt(): void {
    if (!this.dataChannel || this.dataChannel.readyState !== 'open') {
      return
    }

    this.dataChannel.send(JSON.stringify({ type: 'response.cancel' }))
  }

  /**
   * Handle events from the server
   */
  private handleServerEvent(event: any): void {
    console.log('Received event:', event)

    // Emit to registered handlers
    const handlers = this.eventHandlers.get(event.type) || []
    handlers.forEach(handler => handler(event))

    // Also emit to wildcard handlers
    const wildcardHandlers = this.eventHandlers.get('*') || []
    wildcardHandlers.forEach(handler => handler(event))
  }

  /**
   * Emit a custom event to registered handlers
   */
  protected emit(eventType: string, data: any): void {
    const handlers = this.eventHandlers.get(eventType) || []
    handlers.forEach(handler => handler(data))
  }

  /**
   * Register an event handler
   */
  on(eventType: string, handler: Function): void {
    if (!this.eventHandlers.has(eventType)) {
      this.eventHandlers.set(eventType, [])
    }
    this.eventHandlers.get(eventType)!.push(handler)
  }

  /**
   * Unregister an event handler
   */
  off(eventType: string, handler: Function): void {
    const handlers = this.eventHandlers.get(eventType)
    if (handlers) {
      const index = handlers.indexOf(handler)
      if (index > -1) {
        handlers.splice(index, 1)
      }
    }
  }

  /**
   * Disconnect and cleanup
   */
  disconnect(): void {
    if (this.dataChannel) {
      this.dataChannel.close()
      this.dataChannel = null
    }

    if (this.peerConnection) {
      this.peerConnection.close()
      this.peerConnection = null
    }

    if (this.audioElement) {
      this.audioElement.srcObject = null
    }

    this.eventHandlers.clear()
  }

  /**
   * Check if connected
   */
  isConnected(): boolean {
    return this.peerConnection?.connectionState === 'connected'
  }

  /**
   * Get connection state
   */
  getConnectionState(): RTCPeerConnectionState | null {
    return this.peerConnection?.connectionState || null
  }
}

export default new RealtimeService()
