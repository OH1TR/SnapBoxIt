<template>
  <div class="print-page">
    <div class="header">
        <button @click="goBack" class="back-button">← Takaisin</button>
      <h1>Tulosta tarra</h1>
    </div>

    <div class="print-container">
      <div class="form-section">
        <div class="radio-group">
          <label class="radio-label">
            <input
              v-model="printType"
              type="radio"
              value="label"
              class="radio-input"
            />
            <span>Tarra</span>
          </label>
          <label class="radio-label">
            <input
              v-model="printType"
              type="radio"
              value="qrlabel"
              class="radio-input"
            />
            <span>QR-koodi tarra</span>
          </label>
        </div>

        <div class="form-group">
          <label for="printText">Tulostettava teksti</label>
          <textarea
            id="printText"
            v-model="printText"
            rows="6"
            class="textarea-field"
            placeholder="Kirjoita tähän tulostettava teksti..."
          ></textarea>
        </div>

        <button
          @click="print"
          :disabled="!printText || printing"
          class="btn-print"
        >
          ??? {{ printing ? 'Tulostetaan...' : 'Tulosta' }}
        </button>

        <div v-if="success" class="success-message">
          {{ success }}
        </div>

        <div v-if="error" class="error-message">
          {{ error }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import apiService from '../services/apiService'

const router = useRouter()
const printType = ref<'label' | 'qrlabel'>('qrlabel')
const printText = ref<string>('')
const printing = ref<boolean>(false)
const success = ref<string>('')
const error = ref<string>('')

function goBack(): void {
  router.back()
}

async function print(): Promise<void> {
  if (!printText.value.trim()) {
    error.value = 'Syötä tulostettava teksti'
    return
  }

  try {
    printing.value = true
    error.value = ''
    success.value = ''

    const result = await apiService.printLabel(printType.value, printText.value)
    
    if (result) {
      success.value = 'Tulostepyyntö lähetetty onnistuneesti!'
      printText.value = ''
      
      // Clear success message after 3 seconds
      setTimeout(() => {
        success.value = ''
      }, 3000)
    } else {
      error.value = 'Tulostepyynnön lähetys epäonnistui'
    }
  } catch (err) {
    error.value = `Virhe tulostuksessa: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Print error:', err)
  } finally {
    printing.value = false
  }
}
</script>

<style scoped>
/* Most common styles removed - now in global style.css */

.print-page {
  max-width: 600px;
  margin: 0 auto;
  padding: 20px;
}

.print-container {
  background: white;
  border-radius: 12px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.form-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.btn-print {
  padding: 15px;
  background-color: #8b5cf6;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 18px;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-print:hover:not(:disabled) {
  background-color: #7c3aed;
}

.btn-print:disabled {
  background-color: #9ca3af;
  cursor: not-allowed;
}
</style>
