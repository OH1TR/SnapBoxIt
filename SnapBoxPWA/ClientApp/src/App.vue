<template>
  <div id="app">
    <!-- App Header with voice controls -->
    <AppHeader 
      :is-voice-active="isVoiceActive"
      :is-connected="isVoiceConnected"
      :messages="voiceMessages"
      @toggle-voice="toggleVoice"
    />
    
    <main>
      <router-view />
    </main>

    <!-- Background Voice Controller (no UI) -->
    <VoiceController 
      :is-active="isVoiceActive"
      @update:connected="isVoiceConnected = $event"
      @update:messages="voiceMessages = $event"
      @error="handleVoiceError"
    />

    <!-- Error notification -->
    <transition name="fade">
      <div v-if="errorMessage" class="error-notification" @click="errorMessage = ''">
        {{ errorMessage }}
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import AppHeader from './components/AppHeader.vue'
import VoiceController from './components/VoiceController.vue'

interface Message {
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
}

const isVoiceActive = ref(false)
const isVoiceConnected = ref(false)
const voiceMessages = ref<Message[]>([])
const errorMessage = ref('')

function toggleVoice() {
  isVoiceActive.value = !isVoiceActive.value
}

function handleVoiceError(message: string) {
  errorMessage.value = message
  // Auto-dismiss after 5 seconds
  setTimeout(() => {
    errorMessage.value = ''
  }, 5000)
}
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  background: #f5f5f5;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

main {
  flex: 1;
  padding: 20px;
}

/* Error notification */
.error-notification {
  position: fixed;
  bottom: 20px;
  left: 50%;
  transform: translateX(-50%);
  background: #f44336;
  color: white;
  padding: 16px 24px;
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(244, 67, 54, 0.4);
  z-index: 1001;
  cursor: pointer;
  max-width: 90vw;
  text-align: center;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@media (max-width: 768px) {
  main {
    padding: 16px;
  }
}
</style>
