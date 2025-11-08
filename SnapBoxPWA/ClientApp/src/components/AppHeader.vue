<template>
  <header class="app-header">
    <div class="header-content">
      <h1 class="app-title">SnapBox PWA</h1>
      
      <div class="header-actions">
        <!-- Chat History Button -->
        <button 
          v-if="isVoiceActive"
          @click="toggleHistoryPanel" 
          class="header-btn"
          :class="{ active: showHistory }"
          title="Keskusteluhistoria"
        >
          💬
        </button>
        
        <!-- Voice Control Button -->
        <button 
          @click="handleToggleVoice" 
          class="header-btn voice-btn"
          :class="{ active: isVoiceActive, connected: isConnected }"
          :title="isVoiceActive ? 'Sammuta ääniohjaus' : 'Aktivoi ääniohjaus'"
        >
          <span v-if="!isVoiceActive">🎤</span>
          <span v-else-if="isConnected">🔊</span>
          <span v-else>⏸</span>
        </button>
      </div>
    </div>
    
    <!-- Chat History Panel -->
    <transition name="slide-down">
      <div v-if="showHistory && isVoiceActive" class="history-panel">
        <div class="history-header">
          <h3>Keskusteluhistoria</h3>
          <button @click="showHistory = false" class="close-history-btn">×</button>
        </div>
        <div class="history-content">
          <div
            v-for="(message, index) in messages"
            :key="index"
            :class="['history-message', message.role]"
          >
            <div class="history-message-content">
              {{ message.content }}
            </div>
            <div class="history-message-time">{{ getFormattedTime(message.timestamp) }}</div>
          </div>
          <div v-if="messages.length === 0" class="no-messages">
            Ei viestejä vielä
          </div>
        </div>
      </div>
    </transition>
  </header>
</template>

<script setup lang="ts">
import { ref } from 'vue'

interface Message {
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
}

const emit = defineEmits<{
  (e: 'toggle-voice'): void
}>()

defineProps<{
  isVoiceActive: boolean
  isConnected: boolean
  messages: Message[]
}>()

const showHistory = ref(false)

const handleToggleVoice = () => {
  emit('toggle-voice')
  showHistory.value = false
}

const toggleHistoryPanel = () => {
  showHistory.value = !showHistory.value
}

const getFormattedTime = (date: Date): string => {
  return date.toLocaleTimeString('fi-FI', { hour: '2-digit', minute: '2-digit' })
}
</script>

<style scoped>
.app-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.app-title {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 600;
}

.header-actions {
  display: flex;
  gap: 12px;
  align-items: center;
}

.header-btn {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  border: 2px solid rgba(255, 255, 255, 0.3);
  background: rgba(255, 255, 255, 0.1);
  color: white;
  font-size: 24px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s;
  backdrop-filter: blur(10px);
}

.header-btn:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: scale(1.05);
}

.header-btn:active {
  transform: scale(0.95);
}

.header-btn.active {
  background: rgba(255, 255, 255, 0.25);
  border-color: rgba(255, 255, 255, 0.6);
}

.voice-btn.connected {
  background: #4caf50;
  border-color: #66bb6a;
  animation: pulse-glow 2s ease-in-out infinite;
}

@keyframes pulse-glow {
  0%, 100% {
    box-shadow: 0 0 0 0 rgba(76, 175, 80, 0.7);
  }
  50% {
    box-shadow: 0 0 0 8px rgba(76, 175, 80, 0);
  }
}

/* History Panel */
.history-panel {
  background: white;
  color: #333;
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  max-height: 400px;
  display: flex;
  flex-direction: column;
}

.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  border-bottom: 1px solid #e0e0e0;
}

.history-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.close-history-btn {
  background: none;
  border: none;
  font-size: 28px;
  cursor: pointer;
  color: #666;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background 0.2s;
}

.close-history-btn:hover {
  background: #f5f5f5;
}

.history-content {
  flex: 1;
  overflow-y: auto;
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-message {
  display: flex;
  flex-direction: column;
  max-width: 80%;
}

.history-message.user {
  align-self: flex-end;
}

.history-message.assistant {
  align-self: flex-start;
}

.history-message.system {
  align-self: center;
  max-width: 100%;
}

.history-message-content {
  padding: 10px 14px;
  border-radius: 12px;
  word-wrap: break-word;
}

.history-message.user .history-message-content {
  background: #667eea;
  color: white;
}

.history-message.assistant .history-message-content {
  background: #f5f5f5;
  color: #333;
}

.history-message.system .history-message-content {
  background: #e3f2fd;
  color: #1565c0;
  font-size: 13px;
  text-align: center;
}

.history-message-time {
  font-size: 11px;
  color: #999;
  margin-top: 4px;
  padding: 0 14px;
}

.no-messages {
  text-align: center;
  color: #999;
  padding: 40px 20px;
  font-style: italic;
}

/* Transitions */
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
}

.slide-down-enter-from,
.slide-down-leave-to {
  max-height: 0;
  opacity: 0;
}

.slide-down-enter-to,
.slide-down-leave-from {
  max-height: 400px;
  opacity: 1;
}

/* Mobile responsiveness */
@media (max-width: 768px) {
  .app-title {
    font-size: 1.25rem;
  }

  .header-btn {
    width: 44px;
    height: 44px;
    font-size: 20px;
  }

  .header-content {
    padding: 12px 16px;
  }

  .history-panel {
    max-height: 300px;
  }
}
</style>
