<template>
  <!-- Invisible component - no UI, only voice functionality -->
  <div v-if="false"></div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import inventoryRealtimeService from '../services/inventoryRealtimeService'

interface Message {
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
}

const props = defineProps<{
  isActive: boolean
}>()

const emit = defineEmits<{
  (e: 'update:connected', value: boolean): void
  (e: 'update:messages', value: Message[]): void
  (e: 'error', message: string): void
}>()

const realtimeService = inventoryRealtimeService
const messages = ref<Message[]>([])
const isConnected = ref(false)
const isConnecting = ref(false)

// Watch for activation changes
watch(() => props.isActive, async (active) => {
  if (active && !isConnected.value && !isConnecting.value) {
    await connect()
  } else if (!active && isConnected.value) {
    disconnect()
  }
})

async function connect() {
  try {
    isConnecting.value = true
    
    // Set up event handlers
    setupEventHandlers()
    
    // Connect with inventory functions
    await realtimeService.connectWithFunctions()
    
    isConnected.value = true
    emit('update:connected', true)
    
    addMessage('system', 'Ääniohjaus aktivoitu. Voit nyt puhua sovelluksen kanssa!')
    
  } catch (err: any) {
    emit('error', err.message || 'Yhteyden muodostaminen epäonnistui')
    console.error('Connection error:', err)
    isConnected.value = false
    emit('update:connected', false)
  } finally {
    isConnecting.value = false
  }
}

function setupEventHandlers() {
  // Handle user speech transcription
  realtimeService.on('conversation.item.input_audio_transcription.completed', (event: any) => {
    const transcript = event.transcript
    if (transcript) {
      addMessage('user', transcript)
    }
  })

  // Handle assistant audio responses
  realtimeService.on('response.audio_transcript.done', (event: any) => {
    const transcript = event.transcript
    if (transcript) {
      addMessage('assistant', transcript)
    }
  })

  // Handle text responses
  realtimeService.on('response.text.done', (event: any) => {
    const text = event.text
    if (text) {
      addMessage('assistant', text)
    }
  })

  // Handle function calls
  realtimeService.on('response.function_call_arguments.done', (event: any) => {
    const { name } = event
    addMessage('system', `?? Suoritetaan toiminto: ${name}...`)
  })

  // Handle errors
  realtimeService.on('error', (event: any) => {
    emit('error', event.error?.message || 'Tapahtui virhe')
  })

  // Handle disconnection
  realtimeService.on('close', () => {
    isConnected.value = false
    emit('update:connected', false)
    addMessage('system', 'Yhteys katkaistu')
  })
}

function disconnect() {
  realtimeService.disconnect()
  isConnected.value = false
  emit('update:connected', false)
  addMessage('system', 'Ääniohjaus sammutettu')
}

function addMessage(role: 'user' | 'assistant' | 'system', content: string) {
  messages.value.push({
    role,
    content,
    timestamp: new Date()
  })
  
  // Emit updated messages
  emit('update:messages', messages.value)
}

// Cleanup on unmount
onUnmounted(() => {
  if (isConnected.value) {
    disconnect()
  }
})
</script>
