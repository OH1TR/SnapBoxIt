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

      <div v-if="loading" class="loading">
        Haetaan tuloksia...
      </div>

      <div v-else-if="noResults" class="no-results">
        Ei hakutuloksia. Kokeile toista hakusanaa.
      </div>

      <div v-else-if="searchResults.length > 0" class="results">
        <h2>Hakutulokset ({{ searchResults.length }})</h2>
        <div class="results-grid">
          <div
            v-for="result in searchResults"
            :key="result.id"
            class="result-card"
          >
            <div v-if="result.imageUrl" class="result-image">
              <img :src="result.imageUrl" :alt="result.title" />
            </div>
            <div class="result-content">
              <h3>{{ result.title || 'Ei otsikkoa' }}</h3>
              <div class="result-meta">
                <span class="category">{{ result.category }}</span>
                <span class="box-id">📦 {{ result.boxId }}</span>
              </div>
              <p class="description">{{ result.detailedDescription || 'Ei kuvausta' }}</p>
            </div>
          </div>
        </div>
      </div>

      <div v-if="error" class="error-message">
        {{ error }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiService from '../services/apiService'
import type { ItemDto } from '../types'

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
.search-page {
  max-width: 1200px;
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
}

.result-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
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
.box-id {
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
  margin: 0;
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
</style>
