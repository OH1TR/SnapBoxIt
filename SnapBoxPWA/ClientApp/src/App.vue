<template>
  <div id="app">
    <header v-if="$route.path === '/'">
      <h1>SnapBox PWA</h1>
    </header>
    <main>
      <router-view />
    </main>
    
    <!-- Floating Voice Assistant Button -->
    <button 
      v-if="$route.path !== '/voice'" 
      @click="toggleVoiceChat" 
      class="voice-fab"
      :class="{ active: showVoiceChat }"
      title="AI Voice Assistant"
    >
      <span v-if="!showVoiceChat">??</span>
      <span v-else>×</span>
    </button>

    <!-- Voice Chat Component -->
    <VoiceChat v-if="showVoiceChat" @close="showVoiceChat = false" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import VoiceChat from './components/VoiceChat.vue'

const showVoiceChat = ref(false)

function toggleVoiceChat() {
  showVoiceChat.value = !showVoiceChat.value
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
}

header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 20px;
  text-align: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

header h1 {
  margin: 0;
  font-size: 2rem;
}

main {
  padding: 20px;
}

/* Floating Action Button */
.voice-fab {
  position: fixed;
  bottom: 20px;
  right: 20px;
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  font-size: 28px;
  cursor: pointer;
  box-shadow: 0 4px 16px rgba(102, 126, 234, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s;
  z-index: 999;
}

.voice-fab:hover {
  transform: scale(1.1);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.5);
}

.voice-fab:active {
  transform: scale(0.95);
}

.voice-fab.active {
  background: #f44336;
}

.voice-fab.active:hover {
  box-shadow: 0 6px 20px rgba(244, 67, 54, 0.5);
}

@media (max-width: 768px) {
  .voice-fab {
    bottom: 80px;
  }
}
</style>
