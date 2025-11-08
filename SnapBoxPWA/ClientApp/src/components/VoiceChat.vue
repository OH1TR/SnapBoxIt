<template>
  <div class="voice-chat-container">
    <div class="voice-chat-header">
      <h2>AI Voice Assistant</h2>
      <button @click="closeChat" class="close-btn">×</button>
    </div>

    <div class="voice-chat-content">
      <!-- Status indicator -->
      <div class="status-indicator" :class="statusClass">
        <div class="status-dot"></div>
        <span>{{ statusText }}</span>
      </div>

      <!-- Conversation display -->
      <div class="conversation" ref="conversationRef">
        <div
          v-for="(message, index) in messages"
          :key="index"
          :class="['message', message.role]"
        >
          <div class="message-content">
            {{ message.content }}
          </div>
          <div class="message-time">{{ formatTime(message.timestamp) }}</div>
        </div>
      </div>

      <!-- Controls -->
      <div class="controls">
        <div v-if="!isConnected" class="connect-section">
          <div class="voice-options">
            <label>
              Voice:
              <select v-model="selectedVoice">
                <option value="alloy">Alloy</option>
                <option value="echo">Echo</option>
                <option value="shimmer">Shimmer</option>
              </select>
            </label>
          </div>
          <button @click="connect" :disabled="isConnecting" class="connect-btn">
            {{ isConnecting ? 'Connecting...' : 'Start Voice Chat' }}
          </button>
        </div>

        <div v-else class="active-controls">
          <!-- Audio status -->
          <div class="audio-status">
            <div class="audio-visualizer" :class="{ active: isUserSpeaking }">
              <div class="bar"></div>
              <div class="bar"></div>
              <div class="bar"></div>
              <div class="bar"></div>
            </div>
            <span>{{ isUserSpeaking ? 'Listening...' : 'Ready' }}</span>
          </div>

          <!-- Text input for optional text messages -->
          <div class="text-input-section">
            <input
              v-model="textInput"
              @keyup.enter="sendText"
              placeholder="Type a message (optional)..."
              class="text-input"
            />
            <button @click="sendText" :disabled="!textInput.trim()" class="send-btn">
              Send
            </button>
          </div>

          <!-- Action buttons -->
          <div class="action-buttons">
            <button @click="interrupt" class="interrupt-btn">
              Interrupt
            </button>
            <button @click="disconnect" class="disconnect-btn">
              End Chat
            </button>
          </div>
        </div>
      </div>

      <!-- Error display -->
      <div v-if="error" class="error-message">
        {{ error }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import inventoryRealtimeService from '../services/inventoryRealtimeService'

interface Message {
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
}

const emit = defineEmits<{
  (e: 'close'): void
}>()

const realtimeService = inventoryRealtimeService
const messages = ref<Message[]>([])
const conversationRef = ref<HTMLElement | null>(null)
const isConnected = ref(false)
const isConnecting = ref(false)
const isUserSpeaking = ref(false)
const selectedVoice = ref<'alloy' | 'echo' | 'shimmer'>('alloy')
const textInput = ref('')
const error = ref('')

const statusClass = computed(() => {
  if (isConnected.value) return 'connected'
  if (isConnecting.value) return 'connecting'
  return 'disconnected'
})

const statusText = computed(() => {
  if (isConnected.value) return 'Connected - Inventory Functions Active'
  if (isConnecting.value) return 'Connecting...'
  return 'Disconnected'
})

async function connect() {
  try {
    isConnecting.value = true
    error.value = ''

    // Set up event handlers before connecting
    setupEventHandlers()

    // Connect with inventory functions
    await realtimeService.connectWithFunctions()

    isConnected.value = true
    addMessage('system', 'Connected to AI assistant with inventory functions enabled. Try asking to search for items or view box contents!')

  } catch (err: any) {
    error.value = err.message || 'Failed to connect'
    console.error('Connection error:', err)
  } finally {
    isConnecting.value = false
  }
}

function setupEventHandlers() {
  // Handle transcription (user speech)
  realtimeService.on('conversation.item.input_audio_transcription.completed', (event: any) => {
    const transcript = event.transcript
    if (transcript) {
      addMessage('user', transcript)
    }
  })

  // Handle assistant responses
  realtimeService.on('response.audio_transcript.delta', () => {
    // This gives us incremental transcription of the assistant's speech
    // We'll collect these and show the complete response
  })

  realtimeService.on('response.audio_transcript.done', (event: any) => {
    const transcript = event.transcript
    if (transcript) {
      addMessage('assistant', transcript)
    }
  })

  // Handle text responses
  realtimeService.on('response.text.delta', () => {
    // Incremental text response
  })

  realtimeService.on('response.text.done', (event: any) => {
    const text = event.text
    if (text) {
      addMessage('assistant', text)
    }
  })

  // Handle function calls
  realtimeService.on('response.function_call_arguments.done', (event: any) => {
    const { name } = event
    addMessage('system', `?? Calling function: ${name}...`)
  })

  // Handle errors
  realtimeService.on('error', (event: any) => {
    error.value = event.error?.message || 'An error occurred'
  })

  // Monitor speech activity
  realtimeService.on('input_audio_buffer.speech_started', () => {
    isUserSpeaking.value = true
  })

  realtimeService.on('input_audio_buffer.speech_stopped', () => {
    isUserSpeaking.value = false
  })
}

function sendText() {
  if (!textInput.value.trim()) return

  try {
    addMessage('user', textInput.value)
    realtimeService.sendMessage(textInput.value)
    textInput.value = ''
  } catch (err: any) {
    error.value = err.message || 'Failed to send message'
  }
}

function interrupt() {
  realtimeService.interrupt()
}

function disconnect() {
  realtimeService.disconnect()
  isConnected.value = false
  addMessage('system', 'Disconnected from AI assistant.')
}

function closeChat() {
  disconnect()
  emit('close')
}

function addMessage(role: 'user' | 'assistant' | 'system', content: string) {
  messages.value.push({
    role,
    content,
    timestamp: new Date()
  })

  // Scroll to bottom
  nextTick(() => {
    if (conversationRef.value) {
      conversationRef.value.scrollTop = conversationRef.value.scrollHeight
    }
  })
}

function formatTime(date: Date): string {
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

onMounted(() => {
  addMessage('system', 'Welcome to the AI Voice Assistant with Inventory Functions. Click "Start Voice Chat" to begin.')
})

onUnmounted(() => {
  if (isConnected.value) {
    disconnect()
  }
})
</script>

<style scoped>
.voice-chat-container {
  position: fixed;
  bottom: 20px;
  right: 20px;
  width: 400px;
  max-width: 90vw;
  height: 600px;
  max-height: 80vh;
  background: white;
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
  display: flex;
  flex-direction: column;
  z-index: 1000;
}

.voice-chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  border-bottom: 1px solid #e0e0e0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border-radius: 12px 12px 0 0;
}

.voice-chat-header h2 {
  margin: 0;
  font-size: 18px;
}

.close-btn {
  background: none;
  border: none;
  color: white;
  font-size: 28px;
  cursor: pointer;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background 0.2s;
}

.close-btn:hover {
  background: rgba(255, 255, 255, 0.2);
}

.voice-chat-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 16px;
}

.status-indicator {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 8px;
  margin-bottom: 12px;
  font-size: 14px;
}

.status-indicator.connected {
  background: #e8f5e9;
  color: #2e7d32;
}

.status-indicator.connecting {
  background: #fff3e0;
  color: #e65100;
}

.status-indicator.disconnected {
  background: #ffebee;
  color: #c62828;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: currentColor;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.conversation {
  flex: 1;
  overflow-y: auto;
  margin-bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.message {
  display: flex;
  flex-direction: column;
  max-width: 80%;
}

.message.user {
  align-self: flex-end;
}

.message.assistant {
  align-self: flex-start;
}

.message.system {
  align-self: center;
  max-width: 100%;
}

.message-content {
  padding: 10px 14px;
  border-radius: 12px;
  word-wrap: break-word;
}

.message.user .message-content {
  background: #667eea;
  color: white;
}

.message.assistant .message-content {
  background: #f5f5f5;
  color: #333;
}

.message.system .message-content {
  background: #e3f2fd;
  color: #1565c0;
  font-size: 12px;
  text-align: center;
}

.message-time {
  font-size: 11px;
  color: #999;
  margin-top: 4px;
  padding: 0 14px;
}

.controls {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.connect-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.voice-options label {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 14px;
}

.voice-options select {
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 14px;
}

.connect-btn,
.disconnect-btn,
.interrupt-btn,
.send-btn {
  padding: 12px 24px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.connect-btn {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.connect-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.connect-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.active-controls {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.audio-status {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: #f5f5f5;
  border-radius: 8px;
}

.audio-visualizer {
  display: flex;
  gap: 3px;
  height: 20px;
  align-items: flex-end;
}

.audio-visualizer .bar {
  width: 3px;
  background: #ddd;
  border-radius: 2px;
  transition: all 0.2s;
}

.audio-visualizer.active .bar {
  background: #667eea;
  animation: wave 1s ease-in-out infinite;
}

.audio-visualizer .bar:nth-child(1) { animation-delay: 0s; }
.audio-visualizer .bar:nth-child(2) { animation-delay: 0.1s; }
.audio-visualizer .bar:nth-child(3) { animation-delay: 0.2s; }
.audio-visualizer .bar:nth-child(4) { animation-delay: 0.3s; }

@keyframes wave {
  0%, 100% { height: 4px; }
  50% { height: 16px; }
}

.text-input-section {
  display: flex;
  gap: 8px;
}

.text-input {
  flex: 1;
  padding: 10px 14px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
}

.send-btn {
  background: #667eea;
  color: white;
  padding: 10px 20px;
}

.send-btn:hover:not(:disabled) {
  background: #5568d3;
}

.send-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.action-buttons {
  display: flex;
  gap: 8px;
}

.interrupt-btn {
  flex: 1;
  background: #ff9800;
  color: white;
}

.interrupt-btn:hover {
  background: #f57c00;
}

.disconnect-btn {
  flex: 1;
  background: #f44336;
  color: white;
}

.disconnect-btn:hover {
  background: #d32f2f;
}

.error-message {
  margin-top: 12px;
  padding: 12px;
  background: #ffebee;
  color: #c62828;
  border-radius: 8px;
  font-size: 14px;
}
</style>
