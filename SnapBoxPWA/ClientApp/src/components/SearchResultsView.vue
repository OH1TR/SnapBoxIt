<template>
  <div class="search-results-view">
    <div v-if="loading" class="loading">
      {{ loadingMessage }}
    </div>

    <div v-else-if="noResults" class="no-results">
      {{ noResultsMessage }}
    </div>

    <div v-else-if="items.length > 0" class="results">
      <h2 v-if="showTitle">{{ title }}</h2>
      <div class="results-grid">
        <div
          v-for="item in items"
          :key="item.id"
          class="result-card"
          :class="{ 'deleting': deletingItems.has(item.id) }"
        >
          <div 
            class="result-card-content"
            @click="$emit('item-click', item)"
          >
            <div v-if="item.imageUrl" class="result-image">
              <img :src="item.imageUrl" :alt="item.title || 'Item image'" />
            </div>
            <div class="result-content">
              <h3>{{ item.title || 'Ei otsikkoa' }}</h3>
              <div class="result-meta">
                <span class="category">{{ item.category }}</span>
                <span v-if="showBoxId && item.boxId" class="box-id">📦 {{ item.boxId }}</span>
                <span v-if="showCount && item.count" class="count">Määrä: {{ item.count }}</span>
              </div>
              <p class="description">{{ item.detailedDescription || 'Ei kuvausta' }}</p>
              <p v-if="showUserDescription && item.userDescription" class="user-description">
                <em>{{ item.userDescription }}</em>
              </p>
            </div>
          </div>
          <button 
            v-if="showDeleteButton"
            @click.stop="handleDelete(item)" 
            class="delete-button"
            :title="deleteButtonTitle"
            :disabled="deletingItems.has(item.id)"
          >
            <span v-if="deletingItems.has(item.id)">⏳ Poistetaan...</span>
            <span v-else>🗑️ {{ deleteButtonText }}</span>
          </button>
        </div>
      </div>
    </div>

    <div v-if="error || deleteError" class="error-message">
      {{ error || deleteError }}
    </div>

    <!-- Confirmation Dialog -->
    <div v-if="showConfirmDialog" class="dialog-overlay" @click="closeDialog">
      <div class="dialog-content" @click.stop>
        <h3>{{ confirmTitle }}</h3>
        <p>{{ confirmMessage }}</p>
        <div v-if="itemToDelete" class="item-info">
          <strong>{{ itemToDelete.title || 'Ei otsikkoa' }}</strong>
          <span v-if="itemToDelete.boxId" class="box-info">📦 {{ itemToDelete.boxId }}</span>
        </div>
        <div class="dialog-actions">
          <button @click="closeDialog" class="btn-cancel">{{ cancelButtonText }}</button>
          <button @click="confirmDelete" class="btn-confirm">{{ confirmButtonText }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ItemDto } from '../types'
import apiService from '../services/apiService'

interface Props {
  items?: ItemDto[]
  loading?: boolean
  error?: string
  noResults?: boolean
  loadingMessage?: string
  noResultsMessage?: string
  showTitle?: boolean
  titlePrefix?: string
  showBoxId?: boolean
  showCount?: boolean
  showUserDescription?: boolean
  showDeleteButton?: boolean
  deleteButtonText?: string
  deleteButtonTitle?: string
  confirmTitle?: string
  confirmMessage?: string
  confirmButtonText?: string
  cancelButtonText?: string
}

const props = withDefaults(defineProps<Props>(), {
  items: () => [],
  loading: false,
  error: '',
  noResults: false,
  loadingMessage: 'Ladataan...',
  noResultsMessage: 'Ei tuloksia.',
  showTitle: true,
  titlePrefix: '',
  showBoxId: false,
  showCount: false,
  showUserDescription: false,
  showDeleteButton: false,
  deleteButtonText: 'Poista',
  deleteButtonTitle: 'Poista kohde',
  confirmTitle: 'Vahvista poisto',
  confirmMessage: 'Haluatko varmasti poistaa tämän kohteen?',
  confirmButtonText: 'Poista',
  cancelButtonText: 'Peruuta'
})

const emit = defineEmits<{
  'item-click': [item: ItemDto]
  'item-deleted': [item: ItemDto]
  'delete-error': [item: ItemDto, error: string]
}>()

const showConfirmDialog = ref(false)
const itemToDelete = ref<ItemDto | null>(null)
const deletingItems = ref(new Set<string>())
const deleteError = ref<string>('')

const title = computed(() => {
  if (props.titlePrefix) {
    return `${props.titlePrefix} - ${props.items.length} kohdetta`
  }
  return `Tulokset (${props.items.length})`
})

function handleDelete(item: ItemDto): void {
  itemToDelete.value = item
  showConfirmDialog.value = true
}

function closeDialog(): void {
  showConfirmDialog.value = false
  itemToDelete.value = null
}

async function confirmDelete(): Promise<void> {
  if (!itemToDelete.value) return

  const item = itemToDelete.value
  closeDialog()
  
  // Mark item as deleting
  deletingItems.value.add(item.id)
  deleteError.value = ''

  try {
    const success = await apiService.deleteItem(item.id)
    
    if (success) {
      // Clean up the blob URL for the deleted item
      if (item.imageUrl && item.imageUrl.startsWith('blob:')) {
        URL.revokeObjectURL(item.imageUrl)
      }
      
      // Emit success event so parent can remove from array
      emit('item-deleted', item)
    } else {
      deleteError.value = 'Kohteen poistaminen epäonnistui'
      emit('delete-error', item, 'Kohteen poistaminen epäonnistui')
      
      // Clear error after 5 seconds
      setTimeout(() => {
        deleteError.value = ''
      }, 5000)
    }
  } catch (err) {
    const errorMessage = `Virhe poistettaessa kohdetta: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    deleteError.value = errorMessage
    emit('delete-error', item, errorMessage)
    console.error('Delete error:', err)
    
    // Clear error after 5 seconds
    setTimeout(() => {
      deleteError.value = ''
    }, 5000)
  } finally {
    // Remove from deleting set
    deletingItems.value.delete(item.id)
  }
}
</script>

<style scoped>
.search-results-view {
  width: 100%;
}

.loading,
.no-results {
  text-align: center;
  padding: 40px;
  color: #666;
  font-size: 18px;
}

.results h2 {
  color: #2c3e50;
  margin-bottom: 20px;
}

.results-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}

.result-card {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  overflow: hidden;
  transition: all 0.3s;
  display: flex;
  flex-direction: column;
}

.result-card.deleting {
  opacity: 0.6;
  pointer-events: none;
}

.result-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.result-card-content {
  flex: 1;
  cursor: pointer;
}

.result-image {
  width: 100%;
  height: 200px;
  background: #f5f5f5;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.result-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.result-content {
  padding: 15px;
}

.result-content h3 {
  margin: 0 0 10px 0;
  color: #2c3e50;
  font-size: 18px;
}

.result-meta {
  display: flex;
  gap: 10px;
  margin-bottom: 10px;
  flex-wrap: wrap;
}

.category,
.box-id,
.count {
  padding: 4px 12px;
  border-radius: 4px;
  font-size: 14px;
  background: #f0f0f0;
  color: #666;
}

.description {
  color: #666;
  font-size: 14px;
  line-height: 1.5;
  margin: 0 0 10px 0;
}

.user-description {
  color: #0066cc;
  font-size: 14px;
  line-height: 1.5;
  margin: 0;
}

.delete-button {
  width: 100%;
  padding: 12px;
  background-color: #dc3545;
  color: white;
  border: none;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.delete-button:hover:not(:disabled) {
  background-color: #c82333;
}

.delete-button:active:not(:disabled) {
  background-color: #bd2130;
}

.delete-button:disabled {
  background-color: #6c757d;
  cursor: not-allowed;
}

.error-message {
  margin-top: 20px;
  padding: 12px;
  background-color: #fee;
  color: #c00;
  border: 1px solid #fcc;
  border-radius: 6px;
  text-align: center;
}

/* Dialog Styles */
.dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 20px;
}

.dialog-content {
  background: white;
  border-radius: 12px;
  padding: 24px;
  max-width: 500px;
  width: 100%;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

.dialog-content h3 {
  margin: 0 0 12px 0;
  color: #2c3e50;
  font-size: 20px;
}

.dialog-content p {
  margin: 0 0 16px 0;
  color: #666;
  font-size: 16px;
  line-height: 1.5;
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

.dialog-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
}

.btn-cancel,
.btn-confirm {
  padding: 10px 20px;
  border: none;
  border-radius: 6px;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel {
  background-color: #e0e0e0;
  color: #333;
}

.btn-cancel:hover {
  background-color: #d0d0d0;
}

.btn-confirm {
  background-color: #dc3545;
  color: white;
}

.btn-confirm:hover {
  background-color: #c82333;
}

.btn-confirm:active {
  background-color: #bd2130;
}
</style>
