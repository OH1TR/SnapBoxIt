import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useSettingsStore = defineStore('settings', () => {
  // Load from localStorage or use defaults
  const apiBaseUrl = ref<string>(localStorage.getItem('api_base_url') || 'https://api.example.com')
  const username = ref<string>(localStorage.getItem('username') || '')
  const password = ref<string>(localStorage.getItem('password') || '')

  const isConfigured = computed(() => {
    return apiBaseUrl.value && username.value && password.value
  })

  function saveSettings(url: string, user: string, pass: string): void {
    apiBaseUrl.value = url.trim()
    username.value = user.trim()
    password.value = pass

    localStorage.setItem('api_base_url', apiBaseUrl.value)
    localStorage.setItem('username', username.value)
    localStorage.setItem('password', password.value)
  }

  function clearSettings(): void {
    apiBaseUrl.value = 'https://api.example.com'
    username.value = ''
    password.value = ''

    localStorage.removeItem('api_base_url')
    localStorage.removeItem('username')
    localStorage.removeItem('password')
  }

  function getAuthHeader(): string {
    const credentials = btoa(`${username.value}:${password.value}`)
    return `Basic ${credentials}`
  }

  return {
    apiBaseUrl,
    username,
    password,
    isConfigured,
    saveSettings,
    clearSettings,
    getAuthHeader
  }
})
