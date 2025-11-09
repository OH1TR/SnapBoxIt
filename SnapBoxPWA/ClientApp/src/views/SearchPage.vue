<template>
  <div class="search-page">
    <div class="header">
      <button @click="goBack" class="back-button">← Takaisin</button>
      <h1>Hae komponentteja</h1>
    </div>

    <div class="search-container">
      <div class="search-box">
        <input
          v-model="searchQuery"
          @keyup.enter="performSearch"
          type="text"
          placeholder="Syötä hakusana..."
          class="search-input"
        />
        <button @click="performSearch" :disabled="loading || !searchQuery" class="btn-search">
          {{ loading ? 'Haetaan...' : 'Hae' }}
        </button>
      </div>

      <SearchResultsView
        :items="searchResults"
        :loading="loading"
        :error="error"
        :no-results="noResults"
        :loading-message="'Haetaan tuloksia...'"
        :no-results-message="'Ei hakutuloksia. Kokeile toista hakusanaa.'"
        :title-prefix="'Hakutulokset'"
        :show-box-id="true"
        :show-count="false"
        :show-user-description="false"
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
  name: 'SearchPage'
})

const router = useRouter()
const route = useRoute()
const searchQuery = ref<string>('')
const searchResults = ref<ItemDto[]>([])
const loading = ref<boolean>(false)
const noResults = ref<boolean>(false)
const error = ref<string>('')

onMounted(async () => {
  // Check if there's a query parameter from voice navigation
  const queryParam = route.query.q as string
  if (queryParam) {
    searchQuery.value = queryParam
    await performSearch()
  }
})

// Watch for changes in route query (for voice navigation updates)
watch(() => route.query.q, async (newQuery) => {
  if (newQuery && typeof newQuery === 'string') {
    searchQuery.value = newQuery
    await performSearch()
  }
})

onBeforeUnmount(() => {
  // Clean up blob URLs to prevent memory leaks
  cleanupBlobUrls()
})

function goBack(): void {
  router.back()
}

function cleanupBlobUrls(): void {
  searchResults.value.forEach(result => {
    if (result.imageUrl && result.imageUrl.startsWith('blob:')) {
      URL.revokeObjectURL(result.imageUrl)
    }
  })
}

function handleItemClick(item: ItemDto): void {
  // Navigate to edit page with item data
  const itemData = encodeURIComponent(JSON.stringify(item))
  router.push(`/edit/${itemData}`)
}

function handleItemDeleted(item: ItemDto): void {
  // Remove the item from search results
  searchResults.value = searchResults.value.filter(r => r.id !== item.id)
  
  // Update noResults state if needed
  if (searchResults.value.length === 0) {
    noResults.value = true
  }
}

async function performSearch(): Promise<void> {
  if (!searchQuery.value.trim()) return

  try {
    // Clean up previous blob URLs before loading new results
    cleanupBlobUrls()
    
    loading.value = true
    error.value = ''
    noResults.value = false
    searchResults.value = []

    const items = await apiService.searchItems(searchQuery.value, 10)

    if (items && items.length > 0) {
      // Load images for each result
      const resultsWithImages = await Promise.all(
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
      searchResults.value = resultsWithImages
      noResults.value = false
    } else {
      noResults.value = true
    }
  } catch (err) {
    error.value = `Haku epäonnistui: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Search error:', err)
    noResults.value = true
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.search-container {
  background: white;
  border-radius: 12px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.search-box {
  display: flex;
  gap: 10px;
  margin-bottom: 30px;
}

.search-input {
  flex: 1;
  padding: 12px 20px;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  font-size: 16px;
}

.search-input:focus {
  outline: none;
  border-color: #0066cc;
}

.btn-search {
  padding: 12px 30px;
  background-color: #0066cc;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 16px;
  cursor: pointer;
  transition: background 0.2s;
  white-space: nowrap;
}

.btn-search:hover:not(:disabled) {
  background-color: #0052a3;
}

.btn-search:disabled {
  background-color: #9ca3af;
  cursor: not-allowed;
}
</style>
