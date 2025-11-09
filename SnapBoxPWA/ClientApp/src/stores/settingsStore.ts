import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { get, set, del } from 'idb-keyval'

// Storage keys
const STORAGE_KEYS = {
  apiBaseUrl: 'api_base_url',
  username: 'username',
  password: 'password'
}

// Storage helper with IndexedDB and localStorage fallback
async function getStorageItem(key: string, defaultValue: string = ''): Promise<string> {
  try {
    // Try IndexedDB first
    const value = await get(key)
    return value ?? defaultValue
  } catch (error) {
    console.warn('IndexedDB not available, falling back to localStorage:', error)
    // Fallback to localStorage
    return localStorage.getItem(key) || defaultValue
  }
}

async function setStorageItem(key: string, value: string): Promise<void> {
  try {
    // Try IndexedDB first
    await set(key, value)
  } catch (error) {
    console.warn('IndexedDB not available, falling back to localStorage:', error)
    // Fallback to localStorage
    localStorage.setItem(key, value)
  }
}

async function removeStorageItem(key: string): Promise<void> {
  try {
    // Try IndexedDB first
    await del(key)
  } catch (error) {
    console.warn('IndexedDB not available, falling back to localStorage:', error)
    // Fallback to localStorage
    localStorage.removeItem(key)
  }
}

// Migrate existing localStorage data to IndexedDB
async function migrateFromLocalStorage(): Promise<void> {
  try {
    const keys = [STORAGE_KEYS.apiBaseUrl, STORAGE_KEYS.username, STORAGE_KEYS.password]
    
    for (const key of keys) {
      // Check if data exists in localStorage but not in IndexedDB
      const localValue = localStorage.getItem(key)
      const idbValue = await get(key)
      
      if (localValue && !idbValue) {
        console.log(`Migrating ${key} from localStorage to IndexedDB`)
        await set(key, localValue)
        // Optionally remove from localStorage after successful migration
        // localStorage.removeItem(key)
      }
    }
  } catch (error) {
    console.warn('Failed to migrate from localStorage:', error)
  }
}

export const useSettingsStore = defineStore('settings', () => {
  // Initialize with default values, will be loaded asynchronously
  const apiBaseUrl = ref<string>('https://api.example.com')
  const username = ref<string>('')
  const password = ref<string>('')
  const isLoaded = ref<boolean>(false)

  const isConfigured = computed(() => {
    return apiBaseUrl.value && username.value && password.value
  })

  // Load settings from storage
  async function loadSettings(): Promise<void> {
    try {
      // First, migrate any existing localStorage data to IndexedDB
      await migrateFromLocalStorage()
      
      const [url, user, pass] = await Promise.all([
        getStorageItem(STORAGE_KEYS.apiBaseUrl, 'https://api.example.com'),
        getStorageItem(STORAGE_KEYS.username),
        getStorageItem(STORAGE_KEYS.password)
      ])

      apiBaseUrl.value = url
      username.value = user
      password.value = pass
      isLoaded.value = true
    } catch (error) {
      console.error('Failed to load settings:', error)
      isLoaded.value = true
    }
  }

  async function saveSettings(url: string, user: string, pass: string): Promise<void> {
    apiBaseUrl.value = url.trim()
    username.value = user.trim()
    password.value = pass

    await Promise.all([
      setStorageItem(STORAGE_KEYS.apiBaseUrl, apiBaseUrl.value),
      setStorageItem(STORAGE_KEYS.username, username.value),
      setStorageItem(STORAGE_KEYS.password, password.value)
    ])
  }

  async function clearSettings(): Promise<void> {
    apiBaseUrl.value = 'https://api.example.com'
    username.value = ''
    password.value = ''

    await Promise.all([
      removeStorageItem(STORAGE_KEYS.apiBaseUrl),
      removeStorageItem(STORAGE_KEYS.username),
      removeStorageItem(STORAGE_KEYS.password)
    ])
  }

  function getAuthHeader(): string {
    const credentials = btoa(`${username.value}:${password.value}`)
    return `Basic ${credentials}`
  }

  // Auto-load settings on store initialization
  loadSettings()

  return {
    apiBaseUrl,
    username,
    password,
    isConfigured,
    isLoaded,
    loadSettings,
    saveSettings,
    clearSettings,
    getAuthHeader
  }
})
