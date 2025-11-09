<template>
  <div class="settings-page">
    <div class="header">
      <button @click="goBack" class="back-button">← Takaisin</button>
      <h1>Asetukset</h1>
    </div>

    <div class="settings-form">
      <div class="form-group">
        <label for="apiUrl">API URL</label>
        <input
          id="apiUrl"
          v-model="apiUrl"
          type="text"
          placeholder="https://api.example.com"
          class="input-field"
        />
      </div>

      <div class="form-group">
        <label for="username">Käyttäjätunnus</label>
        <input
          id="username"
          v-model="usernameInput"
          type="text"
          placeholder="Käyttäjätunnus"
          class="input-field"
        />
      </div>

      <div class="form-group">
        <label for="password">Salasana</label>
        <input
          id="password"
          v-model="passwordInput"
          type="password"
          placeholder="Salasana"
          class="input-field"
        />
      </div>

      <div class="button-group">
        <button @click="save" class="btn-primary">Tallenna</button>
        <button @click="clear" class="btn-secondary">Tyhjennä</button>
      </div>

      <div v-if="message" :class="['message', messageType]">
        {{ message }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useSettingsStore } from '../stores/settingsStore'

const router = useRouter()
const settingsStore = useSettingsStore()

const apiUrl = ref<string>('')
const usernameInput = ref<string>('')
const passwordInput = ref<string>('')
const message = ref<string>('')
const messageType = ref<'success' | 'info'>('success')

onMounted(async () => {
  // Wait for settings to load if not already loaded
  if (!settingsStore.isLoaded) {
    await settingsStore.loadSettings()
  }
  apiUrl.value = settingsStore.apiBaseUrl
  usernameInput.value = settingsStore.username
  passwordInput.value = settingsStore.password
})

async function save(): Promise<void> {
  await settingsStore.saveSettings(apiUrl.value, usernameInput.value, passwordInput.value)
  message.value = 'Asetukset tallennettu onnistuneesti!'
  messageType.value = 'success'
  setTimeout(() => {
    message.value = ''
  }, 3000)
}

async function clear(): Promise<void> {
  if (confirm('Haluatko varmasti tyhjentää kaikki asetukset?')) {
    await settingsStore.clearSettings()
    apiUrl.value = settingsStore.apiBaseUrl
    usernameInput.value = ''
    passwordInput.value = ''
    message.value = 'Asetukset tyhjennetty!'
    messageType.value = 'info'
    setTimeout(() => {
      message.value = ''
    }, 3000)
  }
}

function goBack(): void {
  router.back()
}
</script>

<style scoped>
.settings-page {
  max-width: 600px;
  margin: 0 auto;
  padding: 20px;
}

.settings-form {
  background: white;
  border-radius: 12px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.message {
  margin-top: 20px;
  padding: 12px;
  border-radius: 6px;
  text-align: center;
}

.message.success {
  background-color: #d1fae5;
  color: #065f46;
}

.message.info {
  background-color: #dbeafe;
  color: #1e40af;
}
</style>
