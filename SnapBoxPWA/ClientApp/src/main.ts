import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import './style.css'
import { useSettingsStore } from './stores/settingsStore'

const pinia = createPinia()
const app = createApp(App)

app.use(pinia)
app.use(router)

// Wait for settings to load before mounting the app
const settingsStore = useSettingsStore()
settingsStore.loadSettings().then(() => {
  app.mount('#app')
})
