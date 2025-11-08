<template>
  <!-- Invisible component - no UI, only voice functionality -->
  <div v-if="false"></div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import type { Router } from 'vue-router'
import inventoryRealtimeService, { type NavigationEvent, type CameraControlEvent, type BoxSelectionEvent } from '../services/inventoryRealtimeService'

interface Message {
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
}

const props = defineProps<{
  isActive: boolean
  router: Router
}>()

const emit = defineEmits<{
  (e: 'update:connected', value: boolean): void
  (e: 'update:messages', value: Message[]): void
  (e: 'error', message: string): void
  (e: 'navigate', event: NavigationEvent): void
  (e: 'camera', event: CameraControlEvent): void
  (e: 'select-box', boxId: string): void
  (e: 'reject'): void
}>()

const realtimeService = inventoryRealtimeService
const messages = ref<Message[]>([])
const isConnected = ref(false)
const isConnecting = ref(false)
const selectedBoxId = ref<string>('')

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

  // Handle navigation events
  realtimeService.on('navigate', (event: NavigationEvent) => {
    console.log('Navigation event received:', event)
    handleNavigation(event)
  })

  // Handle camera control events
  realtimeService.on('camera', (event: CameraControlEvent) => {
    console.log('Camera control event received:', event)
    handleCameraControl(event)
  })

  // Handle box selection events
  realtimeService.on('select_box', (event: BoxSelectionEvent) => {
    console.log('Box selection event received:', event)
    handleBoxSelection(event)
  })

  // Handle reject events
  realtimeService.on('reject_item', () => {
    console.log('Reject event received')
    handleReject()
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

function handleNavigation(event: NavigationEvent) {
  console.log('Handling navigation:', event)
  
  if (event.route === '/search' && event.params?.query) {
    // Navigate to search page with query
    addMessage('system', `?? Siirrytään hakunäkymään hakusanalla: "${event.params.query}"`)
    props.router.push({
      path: '/search',
      query: { q: event.params.query }
    })
  } else if (event.route === '/boxes' && event.params?.boxId) {
    // Navigate to box view with selected box
    addMessage('system', `?? Siirrytään laatikkonäkymään: ${event.params.boxId}`)
    props.router.push({
      path: '/boxes',
      query: { box: event.params.boxId }
    })
  } else if (event.route === '/upload') {
    // Navigate to upload page
    addMessage('system', '?? Siirrytään lataussivulle')
    const query: any = {}
    if (event.params?.boxId) {
      query.box = event.params.boxId
      selectedBoxId.value = event.params.boxId
    } else if (selectedBoxId.value) {
      query.box = selectedBoxId.value
    }
    props.router.push({
      path: '/upload',
      query
    })
  } else {
    // Generic navigation
    addMessage('system', `?? Siirrytään sivulle: ${event.route}`)
    props.router.push(event.route)
  }
  
  // Emit navigation event to parent
  emit('navigate', event)
}

function handleCameraControl(event: CameraControlEvent) {
  console.log('Handling camera control:', event)
  
  if (event.action === 'capture') {
    if (event.boxId) {
      selectedBoxId.value = event.boxId
      addMessage('system', `?? Otetaan kuva laatikkoon: ${event.boxId}`)
      emit('camera', event)
    } else if (selectedBoxId.value) {
      addMessage('system', `?? Otetaan kuva laatikkoon: ${selectedBoxId.value}`)
      emit('camera', { ...event, boxId: selectedBoxId.value })
    } else {
      addMessage('system', '?? Laatikko pitää valita ensin ennen kuvan ottamista!')
      emit('error', 'Valitse laatikko ensin')
    }
  }
}

function handleBoxSelection(event: BoxSelectionEvent) {
  console.log('Handling box selection:', event)
  
  // Always update selected box (allow re-selection)
  const previousBox = selectedBoxId.value
  selectedBoxId.value = event.boxId
  
  if (previousBox && previousBox !== event.boxId) {
    addMessage('system', `?? Laatikko vaihdettu: ${previousBox} ? ${event.boxId}`)
  } else {
    addMessage('system', `?? Laatikko ${event.boxId} valittu`)
  }
  
  emit('select-box', event.boxId)
}

function handleReject() {
  console.log('Handling reject command')
  addMessage('system', '??? Hylätään kohde...')
  emit('reject')
}

function disconnect() {
  realtimeService.disconnect()
  isConnected.value = false
  emit('update:connected', false)
  addMessage('system', 'Ääniohjaus sammutettu')
  selectedBoxId.value = ''
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
