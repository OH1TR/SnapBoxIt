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
.print-page {
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

.radio-group {
  display: flex;
  gap: 20px;
  padding: 15px;
  background: #f9f9f9;
  border-radius: 8px;
}

.radio-label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 16px;
  color: #2c3e50;
}

.radio-input {
  cursor: pointer;
  width: 18px;
  height: 18px;
}

.form-group {
  display: flex;
  flex-direction: column;
}

.form-group label {
  margin-bottom: 8px;
  color: #2c3e50;
  font-weight: 500;
}

.textarea-field {
  width: 100%;
  padding: 12px;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  font-size: 16px;
  font-family: inherit;
  resize: vertical;
  box-sizing: border-box;
}

.textarea-field:focus {
  outline: none;
  border-color: #0066cc;
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

.success-message {
  padding: 12px;
  background-color: #d1fae5;
  color: #065f46;
  border: 1px solid #a7f3d0;
  border-radius: 6px;
  text-align: center;
}

.error-message {
  padding: 12px;
  background-color: #fee;
  color: #c00;
  border: 1px solid #fcc;
  border-radius: 6px;
  text-align: center;
}
</style>
