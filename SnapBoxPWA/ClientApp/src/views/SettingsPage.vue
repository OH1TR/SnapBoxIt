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

onMounted(() => {
  apiUrl.value = settingsStore.apiBaseUrl
  usernameInput.value = settingsStore.username
  passwordInput.value = settingsStore.password
})

function save(): void {
  settingsStore.saveSettings(apiUrl.value, usernameInput.value, passwordInput.value)
  message.value = 'Asetukset tallennettu onnistuneesti!'
  messageType.value = 'success'
  setTimeout(() => {
    message.value = ''
  }, 3000)
}

function clear(): void {
  if (confirm('Haluatko varmasti tyhjentää kaikki asetukset?')) {
    settingsStore.clearSettings()
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

.header {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 30px;
}

.back-button {
  background: #f0f0f0;
  border: none;
  padding: 10px 15px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 16px;
  transition: background 0.2s;
}

.back-button:hover {
  background: #e0e0e0;
}

.header h1 {
  margin: 0;
  color: #2c3e50;
}

.settings-form {
  background: white;
  border-radius: 12px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  color: #2c3e50;
  font-weight: 500;
}

.input-field {
  width: 100%;
  padding: 12px;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  font-size: 16px;
  box-sizing: border-box;
  transition: border-color 0.2s;
}

.input-field:focus {
  outline: none;
  border-color: #0066cc;
}

.button-group {
  display: flex;
  gap: 10px;
  margin-top: 30px;
}

.btn-primary,
.btn-secondary {
  flex: 1;
  padding: 12px 20px;
  border: none;
  border-radius: 6px;
  font-size: 16px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background-color: #0066cc;
  color: white;
}

.btn-primary:hover {
  background-color: #0052a3;
}

.btn-secondary {
  background-color: #6b7280;
  color: white;
}

.btn-secondary:hover {
  background-color: #4b5563;
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
