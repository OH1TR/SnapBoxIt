<template>
  <div class="box-view-page">
    <div class="header">
      <button @click="goBack" class="back-button">← Takaisin</button>
      <h1>Laatikot</h1>
    </div>

    <div class="box-view-container">
      <div class="box-selector">
        <label for="boxSelect">Valitse laatikko</label>
        <select
          id="boxSelect"
          v-model="selectedBox"
          @change="loadBoxContents"
          class="box-select"
        >
          <option value="">-- Valitse laatikko --</option>
          <option v-for="box in boxes" :key="box" :value="box">
            {{ box }}
          </option>
        </select>
      </div>

      <div v-if="loadingBoxes" class="loading">
        Ladataan laatikoita...
      </div>

      <SearchResultsView
        v-else
        :items="boxContents"
        :loading="loadingContents"
        :error="error"
        :no-results="noResults"
        :loading-message="'Ladataan laatikon sisältöä...'"
        :no-results-message="'Laatikko on tyhjä tai sitä ei löytynyt.'"
        :title-prefix="selectedBox"
        :show-count="true"
        :show-user-description="true"
        :show-box-id="false"
        :show-delete-button="true"
        @item-click="handleItemClick"
        @item-deleted="handleItemDeleted"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiService from '../services/apiService'
import SearchResultsView from '../components/SearchResultsView.vue'
import type { ItemDto } from '../types'

// Define component name for keep-alive
defineOptions({
  name: 'BoxViewPage'
})

const router = useRouter()
const route = useRoute()
const boxes = ref<string[]>([])
const selectedBox = ref<string>('')
const boxContents = ref<ItemDto[]>([])
const loadingBoxes = ref<boolean>(false)
const loadingContents = ref<boolean>(false)
const noResults = ref<boolean>(false)
const error = ref<string>('')

onMounted(async () => {
  await loadBoxes()
  
  // Check if there's a box parameter from voice navigation
  const boxParam = route.query.box as string
  if (boxParam && boxes.value.includes(boxParam)) {
    selectedBox.value = boxParam
    await loadBoxContents()
  }
})

// Watch for changes in route query (for voice navigation updates)
watch(() => route.query.box, async (newBox) => {
  if (newBox && typeof newBox === 'string' && boxes.value.includes(newBox)) {
    selectedBox.value = newBox
    await loadBoxContents()
  }
})

onBeforeUnmount(() => {
  // Clean up blob URLs to prevent memory leaks
  cleanupBlobUrls()
})

function cleanupBlobUrls(): void {
  boxContents.value.forEach(item => {
    if (item.imageUrl && item.imageUrl.startsWith('blob:')) {
      URL.revokeObjectURL(item.imageUrl)
    }
  })
}

function goBack(): void {
  router.back()
}

function handleItemClick(item: ItemDto): void {
  // Navigate to edit page with item data
  const itemData = encodeURIComponent(JSON.stringify(item))
  router.push(`/edit/${itemData}`)
}

function handleItemDeleted(item: ItemDto): void {
  // Remove the item from box contents
  boxContents.value = boxContents.value.filter(i => i.id !== item.id)
  
  // Update noResults state if needed
  if (boxContents.value.length === 0) {
    noResults.value = true
  }
}

async function loadBoxes(): Promise<void> {
  try {
    loadingBoxes.value = true
    error.value = ''
    
    const boxList = await apiService.getBoxes()
    boxes.value = boxList || []
  } catch (err) {
    error.value = `Laatikoiden lataus epäonnistui: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Load boxes error:', err)
  } finally {
    loadingBoxes.value = false
  }
}

async function loadBoxContents(): Promise<void> {
  if (!selectedBox.value) {
    boxContents.value = []
    noResults.value = false
    return
  }

  try {
    // Clean up previous blob URLs before loading new content
    cleanupBlobUrls()
    
    loadingContents.value = true
    error.value = ''
    noResults.value = false
    boxContents.value = []
    
    const items = await apiService.getBoxContents(selectedBox.value)

    if (items && items.length > 0) {
      // Load images for each item
      const itemsWithImages = await Promise.all(
        items.map(async (item) => {
          try {
            // Check if blobId exists before trying to load image
            if (item.blobId) {
              const imageBytes = await apiService.getImageBytes(item.blobId, true)
              const blob = new Blob([imageBytes], { type: 'image/jpeg' })
              const imageUrl = URL.createObjectURL(blob)
              return {
                ...item,
                imageUrl
              }
            } else {
              return {
                ...item,
                imageUrl: undefined
              }
            }
          } catch (err) {
            console.error('Error loading image for item:', item.id, err)
            return {
              ...item,
              imageUrl: undefined
            }
          }
        })
      )
      boxContents.value = itemsWithImages
      noResults.value = false
    } else {
      noResults.value = true
    }
  } catch (err) {
    error.value = `Laatikon sisällön lataus epäonnistui: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Load box contents error:', err)
    noResults.value = true
  } finally {
    loadingContents.value = false
  }
}
</script>

<style scoped>
/* Most common styles removed - now in global style.css */

.box-view-container {
  background: white;
  border-radius: 12px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.box-selector {
  margin-bottom: 30px;
}

.box-selector label {
  display: block;
  margin-bottom: 10px;
  color: #2c3e50;
  font-weight: 500;
  font-size: 18px;
}

.box-select {
  width: 100%;
  padding: 12px 20px;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  font-size: 16px;
  cursor: pointer;
}

.box-select:focus {
  outline: none;
  border-color: #0066cc;
}
</style>
