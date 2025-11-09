<template>
  <div class="edit-page">
    <div class="header">
      <button @click="goBack" class="back-button">← Takaisin</button>
      <h1>Muokkaa kohdetta</h1>
    </div>

    <div v-if="loading" class="loading-container">
      <p>Ladataan kohteen tietoja...</p>
    </div>

    <div v-else-if="error && !item" class="error-container">
      <div class="error-message">
        {{ error }}
      </div>
      <button @click="goBack" class="btn-secondary">Takaisin</button>
    </div>

    <div v-else-if="item" class="edit-container">
      <div class="content-layout">
        <!-- Image section -->
        <div class="image-section">
          <div v-if="item.imageUrl" class="item-image">
            <img :src="item.imageUrl" :alt="item.title || 'Item image'" />
          </div>
          <div v-else class="no-image">
            <p>Ei kuvaa saatavilla</p>
          </div>
        </div>

        <!-- Details section -->
        <div class="details-section">
          <h2>Kohteen tiedot</h2>
          
          <div class="detail-group">
            <label>Otsikko</label>
            <p class="readonly-field">{{ item.title || 'Ei otsikkoa' }}</p>
          </div>

          <div class="detail-group">
            <label>Kategoria</label>
            <p class="readonly-field">{{ item.category || 'Ei kategoriaa' }}</p>
          </div>

          <div class="detail-group">
            <label>Kuvaus</label>
            <p class="readonly-field">{{ item.detailedDescription || 'Ei kuvausta' }}</p>
          </div>

          <div class="detail-group">
            <label>Värit</label>
            <p class="readonly-field">{{ item.colors?.join(', ') || 'Ei värejä' }}</p>
          </div>

          <div class="detail-group">
            <label>Luotu</label>
            <p class="readonly-field">{{ formatDate(item.createdAt) }}</p>
          </div>

          <div v-if="item.updatedAt" class="detail-group">
            <label>Päivitetty</label>
            <p class="readonly-field">{{ formatDate(item.updatedAt) }}</p>
          </div>

          <hr class="divider" />

          <h3>Muokattavat tiedot</h3>

          <div class="form-group">
            <label for="boxId">Laatikon tunnus *</label>
            <input
              id="boxId"
              v-model="editableItem.boxId"
              type="text"
              placeholder="Esim. BOX001"
              class="input-field"
              :disabled="saving"
            />
          </div>

          <div class="form-group">
            <label for="count">Määrä *</label>
            <input
              id="count"
              v-model.number="editableItem.count"
              type="number"
              step="0.1"
              min="0"
              class="input-field"
              :disabled="saving"
            />
          </div>

          <div class="form-group">
            <label for="userDescription">Oma kuvaus</label>
            <textarea
              id="userDescription"
              v-model="editableItem.userDescription"
              rows="4"
              class="textarea-field"
              placeholder="Lisää oma kuvaus..."
              :disabled="saving"
            ></textarea>
          </div>

          <div v-if="error" class="error-message">
            {{ error }}
          </div>

          <div class="button-group">
            <button @click="handleCancel" :disabled="saving || deleting" class="btn-secondary">
              Peruuta
            </button>
            <button @click="handleSave" :disabled="saving || deleting || !isValid" class="btn-primary">
              {{ saving ? 'Tallennetaan...' : 'Tallenna' }}
            </button>
          </div>

          <hr class="divider" />

          <div class="danger-zone">
            <h3>Vaarallinen alue</h3>
            <p>Kohteen poistaminen on pysyvä toimenpide eikä sitä voi peruuttaa.</p>
            <button @click="showDeleteConfirm = true" :disabled="saving || deleting" class="btn-danger">
              {{ deleting ? 'Poistetaan...' : '🗑️ Poista kohde' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Dialog -->
    <div v-if="showDeleteConfirm" class="dialog-overlay" @click="showDeleteConfirm = false">
      <div class="dialog-content" @click.stop>
        <h3>Vahvista poisto</h3>
        <p>Haluatko varmasti poistaa tämän kohteen? Tätä toimintoa ei voi peruuttaa.</p>
        <div v-if="item" class="item-info">
          <strong>{{ item.title || 'Ei otsikkoa' }}</strong>
          <span v-if="item.boxId" class="box-info">📦 {{ item.boxId }}</span>
        </div>
        <div class="dialog-actions">
          <button @click="showDeleteConfirm = false" class="btn-cancel">Peruuta</button>
          <button @click="handleDelete" class="btn-confirm">Poista</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiService from '../services/apiService'
import type { ItemDto } from '../types'

const router = useRouter()
const route = useRoute()

const item = ref<ItemDto | null>(null)
const editableItem = ref<{
  boxId: string
  count: number
  userDescription: string
}>({
  boxId: '',
  count: 0,
  userDescription: ''
})

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const error = ref('')
const showDeleteConfirm = ref(false)
const hasChanges = ref(false)

// Validation
const isValid = computed(() => {
  return editableItem.value.boxId.trim() !== '' && 
         editableItem.value.count >= 0 &&
         hasChanges.value
})

onMounted(async () => {
  const itemData = route.params.item as string | undefined
  
  if (!itemData) {
    error.value = 'Kohdetta ei määritelty'
    return
  }

  try {
    // Parse the item from route params
    const parsedItem = JSON.parse(decodeURIComponent(itemData)) as ItemDto
    
    // Load the image if blobId exists
    if (parsedItem.blobId) {
      loading.value = true
      try {
        const imageBytes = await apiService.getImageBytes(parsedItem.blobId, false)
        const blob = new Blob([imageBytes], { type: 'image/jpeg' })
        parsedItem.imageUrl = URL.createObjectURL(blob)
      } catch (err) {
        console.error('Error loading image:', err)
        // Continue without image
      } finally {
        loading.value = false
      }
    }

    item.value = parsedItem
    
    // Initialize editable fields
    editableItem.value = {
      boxId: parsedItem.boxId || '',
      count: parsedItem.count || 0,
      userDescription: parsedItem.userDescription || ''
    }

    // Watch for changes
    watchForChanges()
  } catch (err) {
    error.value = 'Virheellinen kohdetieto'
    console.error('Error parsing item:', err)
  }
})

onBeforeUnmount(() => {
  // Clean up blob URL
  if (item.value?.imageUrl && item.value.imageUrl.startsWith('blob:')) {
    URL.revokeObjectURL(item.value.imageUrl)
  }
})

function watchForChanges() {
  // Simple change detection
  const checkChanges = () => {
    if (!item.value) return false
    
    return editableItem.value.boxId !== (item.value.boxId || '') ||
           editableItem.value.count !== (item.value.count || 0) ||
           editableItem.value.userDescription !== (item.value.userDescription || '')
  }

  // Watch reactive changes
  const interval = setInterval(() => {
    hasChanges.value = checkChanges()
  }, 100)

  onBeforeUnmount(() => clearInterval(interval))
}

function formatDate(date: any): string {
  if (!date) return 'Ei tiedossa'
  
  try {
    const d = new Date(date)
    return d.toLocaleString('fi-FI', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch {
    return 'Virheellinen päivämäärä'
  }
}

function goBack(): void {
  if (hasChanges.value) {
    const confirmed = confirm('Sinulla on tallentamattomia muutoksia. Haluatko varmasti poistua?')
    if (!confirmed) return
  }
  
  router.back()
}

function handleCancel(): void {
  if (hasChanges.value) {
    const confirmed = confirm('Sinulla on tallentamattomia muutoksia. Haluatko varmasti peruuttaa?')
    if (!confirmed) return
  }
  
  router.back()
}

async function handleSave(): Promise<void> {
  if (!item.value || !isValid.value) return

  try {
    saving.value = true
    error.value = ''

    // Create the updated item
    const updatedItem: ItemDto = {
      ...item.value,
      boxId: editableItem.value.boxId.trim(),
      count: editableItem.value.count,
      userDescription: editableItem.value.userDescription.trim()
    }

    const success = await apiService.saveItem(updatedItem)
    
    if (success) {
      // Update local item
      item.value = updatedItem
      
      // Reset editable item
      editableItem.value = {
        boxId: updatedItem.boxId || '',
        count: updatedItem.count || 0,
        userDescription: updatedItem.userDescription || ''
      }
      
      hasChanges.value = false
      
      // Show success message
      alert('Kohde tallennettu onnistuneesti!')
      
      // Go back to previous page
      router.back()
    } else {
      error.value = 'Tallentaminen epäonnistui'
    }
  } catch (err) {
    error.value = `Tallentaminen epäonnistui: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Save error:', err)
  } finally {
    saving.value = false
  }
}

async function handleDelete(): Promise<void> {
  if (!item.value) return

  showDeleteConfirm.value = false

  try {
    deleting.value = true
    error.value = ''

    const success = await apiService.deleteItem(item.value.id)
    
    if (success) {
      // Clean up blob URL
      if (item.value.imageUrl && item.value.imageUrl.startsWith('blob:')) {
        URL.revokeObjectURL(item.value.imageUrl)
      }
      
      alert('Kohde poistettu onnistuneesti!')
      router.back()
    } else {
      error.value = 'Poistaminen epäonnistui'
    }
  } catch (err) {
    error.value = `Poistaminen epäonnistui: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Delete error:', err)
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
/* Most common styles removed - now in global style.css */

.edit-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.edit-container {
  background: white;
  border-radius: 12px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.content-layout {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 40px;
}

@media (max-width: 968px) {
  .content-layout {
    grid-template-columns: 1fr;
  }
}

.image-section {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.item-image {
  width: 100%;
  max-width: 500px;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #e0e0e0;
  background: #f5f5f5;
}

.item-image img {
  width: 100%;
  height: auto;
  display: block;
  object-fit: contain;
  max-height: 600px;
}

.no-image {
  width: 100%;
  max-width: 500px;
  height: 400px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f5f5;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  color: #999;
}

.details-section h2 {
  color: #2c3e50;
  margin-bottom: 20px;
  font-size: 24px;
}

.details-section h3 {
  color: #2c3e50;
  margin-bottom: 15px;
  font-size: 18px;
}

.danger-zone {
  margin-top: 30px;
  padding: 20px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
}

.danger-zone h3 {
  color: #dc2626;
  margin-bottom: 10px;
}

.danger-zone p {
  color: #991b1b;
  font-size: 14px;
  margin-bottom: 15px;
}

.danger-zone .btn-danger {
  width: 100%;
}

.item-info {
  background: #f8f9fa;
  padding: 12px;
  border-radius: 6px;
  margin-bottom: 20px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.item-info strong {
  color: #2c3e50;
  font-size: 16px;
}

.item-info .box-info {
  color: #666;
  font-size: 14px;
}
</style>
