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

      <div v-else-if="loadingContents" class="loading">
        Ladataan laatikon sisältöä...
      </div>

      <div v-else-if="noResults" class="no-results">
        Laatikko on tyhjä tai sitä ei löytynyt.
      </div>

      <div v-else-if="boxContents.length > 0" class="results">
        <h2>{{ selectedBox }} - {{ boxContents.length }} kohdetta</h2>
        <div class="results-grid">
          <div
            v-for="item in boxContents"
            :key="item.id"
            class="result-card"
          >
            <div v-if="item.imageUrl" class="result-image">
              <img :src="item.imageUrl" :alt="item.title" />
            </div>
            <div class="result-content">
              <h3>{{ item.title || 'Ei otsikkoa' }}</h3>
              <div class="result-meta">
                <span class="category">{{ item.category }}</span>
                <span v-if="item.count" class="count">Määrä: {{ item.count }}</span>
              </div>
              <p class="description">{{ item.detailedDescription || 'Ei kuvausta' }}</p>
              <p v-if="item.userDescription" class="user-description">
                <em>{{ item.userDescription }}</em>
              </p>
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

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue';
import { useRouter } from 'vue-router';
import apiService from '../services/apiService';

const router = useRouter();
const boxes = ref([]);
const selectedBox = ref('');
const boxContents = ref([]);
const loadingBoxes = ref(false);
const loadingContents = ref(false);
const noResults = ref(false);
const error = ref('');

onMounted(async () => {
  await loadBoxes();
});

onBeforeUnmount(() => {
  // Clean up blob URLs to prevent memory leaks
  cleanupBlobUrls();
});

function cleanupBlobUrls() {
  boxContents.value.forEach(item => {
    if (item.imageUrl && item.imageUrl.startsWith('blob:')) {
      URL.revokeObjectURL(item.imageUrl);
    }
  });
}

function goBack() {
  router.back();
}

async function loadBoxes() {
  try {
    loadingBoxes.value = true;
    error.value = '';
    
    const boxList = await apiService.getBoxes();
    boxes.value = boxList || [];
  } catch (err) {
    error.value = `Laatikoiden lataus epäonnistui: ${err.message}`;
    console.error('Load boxes error:', err);
  } finally {
    loadingBoxes.value = false;
  }
}

async function loadBoxContents() {
  if (!selectedBox.value) {
    boxContents.value = [];
    noResults.value = false;
    return;
  }

  try {
    // Clean up previous blob URLs before loading new content
    cleanupBlobUrls();
    
    loadingContents.value = true;
    error.value = '';
    noResults.value = false;
    boxContents.value = [];
    
    const items = await apiService.getBoxContents(selectedBox.value);

    if (items && items.length > 0) {
      // Load images for each item
      const itemsWithImages = await Promise.all(
        items.map(async (item) => {
          try {
            // Check if blobId exists before trying to load image
            if (item.blobId) {
              const imageBytes = await apiService.getImageBytes(item.blobId, true);
              const blob = new Blob([imageBytes], { type: 'image/jpeg' });
              const imageUrl = URL.createObjectURL(blob);
              return {
                ...item,
                imageUrl
              };
            } else {
              return {
                ...item,
                imageUrl: null
              };
            }
          } catch (err) {
            console.error('Error loading image for item:', item.id, err);
            return {
              ...item,
              imageUrl: null
            };
          }
        })
      );
      boxContents.value = itemsWithImages;
      noResults.value = false;
    } else {
      noResults.value = true;
    }
  } catch (err) {
    error.value = `Laatikon sisällön lataus epäonnistui: ${err.message}`;
    console.error('Load box contents error:', err);
    noResults.value = true;
  } finally {
    loadingContents.value = false;
  }
}
</script>

<style scoped>
.box-view-page {
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
