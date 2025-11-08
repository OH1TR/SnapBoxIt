<template>
  <div class="voice-assistant-page">
    <div class="page-header">
      <h1>AI Voice Assistant</h1>
      <p>Use voice or text to interact with your SnapBox inventory</p>
    </div>

    <VoiceChat v-if="showChat" @close="handleClose" />

    <div v-if="!showChat" class="start-section">
      <div class="feature-grid">
        <div class="feature-card">
          <div class="feature-icon">??</div>
          <h3>Voice Control</h3>
          <p>Speak naturally to search items, add notes, or get help with your inventory</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon">??</div>
          <h3>Text Chat</h3>
          <p>Prefer typing? You can also send text messages to the assistant</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon">??</div>
          <h3>AI Powered</h3>
          <p>Powered by OpenAI's latest real-time voice technology</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon">??</div>
          <h3>Inventory Help</h3>
          <p>Get assistance organizing boxes, finding items, and managing your storage</p>
        </div>
      </div>

      <button @click="showChat = true" class="start-btn">
        Start Voice Assistant
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import VoiceChat from '../components/VoiceChat.vue'

const showChat = ref(false)

function handleClose() {
  showChat.value = false
}
</script>

<style scoped>
.voice-assistant-page {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  text-align: center;
  margin-bottom: 40px;
}

.page-header h1 {
  font-size: 32px;
  color: #333;
  margin: 0 0 10px 0;
}

.page-header p {
  font-size: 16px;
  color: #666;
  margin: 0;
}

.start-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 40px;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 24px;
  width: 100%;
}

.feature-card {
  background: white;
  padding: 24px;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  text-align: center;
  transition: transform 0.2s, box-shadow 0.2s;
}

.feature-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
}

.feature-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.feature-card h3 {
  font-size: 20px;
  color: #333;
  margin: 0 0 12px 0;
}

.feature-card p {
  font-size: 14px;
  color: #666;
  line-height: 1.5;
  margin: 0;
}

.start-btn {
  padding: 16px 48px;
  font-size: 18px;
  font-weight: 600;
  color: white;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: 0 4px 16px rgba(102, 126, 234, 0.3);
}

.start-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
}

.start-btn:active {
  transform: translateY(0);
}

@media (max-width: 768px) {
  .page-header h1 {
    font-size: 24px;
  }

  .feature-grid {
    grid-template-columns: 1fr;
  }

  .start-btn {
    width: 100%;
  }
}
</style>
