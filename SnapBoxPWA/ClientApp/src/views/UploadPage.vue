<template>
  <div class="upload-page">
    <div class="header">
      <button @click="goBack" class="back-button">← Takaisin</button>
      <h1>Lataa kuva</h1>
    </div>

    <div class="upload-container">
      <div class="form-section">
        <div class="form-group">
          <label for="boxId">Laatikon tunnus</label>
          <input
            id="boxId"
            v-model="boxId"
            type="text"
            placeholder="Esim. BOX001"
            class="input-field"
          />
        </div>

        <div class="camera-section">
          <!-- Camera compatibility warning -->
          <div v-if="!isCameraSupported" class="warning-message">
            <p><strong>Kamera ei ole käytettävissä</strong></p>
            <p v-if="!isSecureContext">Kamera vaatii HTTPS-yhteyden. Käytä osoitetta https://localhost tai palvelinta HTTPS:llä.</p>
            <p v-else>Selaimesi ei tue kameran käyttöä tai kameraa ei löydy.</p>
          </div>

          <div v-if="cameraActive || previewImage" class="camera-container">
            <video
              v-if="cameraActive"
              ref="videoElement"
              autoplay
              playsinline
              class="camera-video"
            ></video>
            
            <div v-if="previewImage && !cameraActive" class="preview-section">
              <img :src="previewImage" alt="Preview" class="preview-image" />
            </div>
            
            <div class="camera-controls">
              <button @click="capturePhoto" class="btn-capture" :disabled="!boxId || (!cameraActive && uploading)">
                📷 Ota kuva
              </button>
            </div>
          </div>

          <canvas ref="canvasElement" style="display: none"></canvas>
        </div>
      </div>

      <div v-if="uploadedItem" class="item-details">
        <h2>Kuva ladattu onnistuneesti!</h2>
        
        <div class="detail-group">
          <label>Otsikko</label>
          <p>{{ uploadedItem.title || 'Ei otsikkoa' }}</p>
        </div>

        <div class="detail-group">
          <label>Kategoria</label>
          <p>{{ uploadedItem.category || 'Ei kategoriaa' }}</p>
        </div>

        <div class="detail-group">
          <label>Kuvaus</label>
          <p>{{ uploadedItem.detailedDescription || 'Ei kuvausta' }}</p>
        </div>

        <div class="detail-group">
          <label>Värit</label>
          <p>{{ uploadedItem.colors?.join(', ') || 'Ei värejä' }}</p>
        </div>

        <div class="form-group">
          <label for="count">Määrä</label>
          <input
            id="count"
            v-model.number="uploadedItem.count"
            type="number"
            step="0.1"
            class="input-field"
          />
        </div>

        <div class="form-group">
          <label for="userDescription">Oma kuvaus</label>
          <textarea
            id="userDescription"
            v-model="uploadedItem.userDescription"
            rows="4"
            class="textarea-field"
            placeholder="Lisää oma kuvaus..."
          ></textarea>
        </div>

        <div class="button-group">
          <button @click="saveItem" :disabled="saving" class="btn-primary">
            {{ saving ? 'Tallennetaan...' : 'Tallenna' }}
          </button>
          <button @click="rejectItem" :disabled="deleting" class="btn-danger">
            {{ deleting ? 'Hylätään...' : 'Hylkää' }}
          </button>
        </div>
      </div>

      <div v-if="error" class="error-message">
        {{ error }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onBeforeUnmount, onMounted, computed, nextTick, watch, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiService from '../services/apiService'
import type { ItemDto } from '../types'

const router = useRouter()
const route = useRoute()
const videoElement = ref<HTMLVideoElement | null>(null)
const canvasElement = ref<HTMLCanvasElement | null>(null)
const boxId = ref<string>('')
const previewImage = ref<string | null>(null)
const uploadedItem = ref<ItemDto | null>(null)
const uploading = ref<boolean>(false)
const saving = ref<boolean>(false)
const deleting = ref<boolean>(false)
const error = ref<string>('')
const cameraActive = ref<boolean>(false)
const isSecureContext = ref<boolean>(false)
const hasCameraSupport = ref<boolean>(false)
let mediaStream: MediaStream | null = null
let previewTimeout: number | null = null

// Inject voice-controlled box selection and camera trigger
const voiceSelectedBoxId = inject<any>('voiceSelectedBoxId', ref(''))
const cameraTrigger = inject<any>('cameraTrigger', ref(0))
const voiceReject = inject<any>('voiceReject', ref(0))
const voiceUpdateDescription = inject<any>('voiceUpdateDescription', ref(''))
const voiceUpdateCount = inject<any>('voiceUpdateCount', ref(null))
const voiceUpdateTrigger = inject<any>('voiceUpdateTrigger', ref(0))

// Watch for voice-selected box ID
watch(() => voiceSelectedBoxId.value, (newBoxId) => {
  if (newBoxId) {
    boxId.value = newBoxId
    console.log('Box ID set from voice command:', newBoxId)
  }
})

// Watch for camera trigger from voice command
watch(() => cameraTrigger.value, async () => {
  if (cameraTrigger.value > 0) {
    console.log('Camera trigger from voice command')
    if (!cameraActive.value && isCameraSupported.value) {
      await startCamera()
      // Wait a bit for camera to be ready
      await new Promise(resolve => setTimeout(resolve, 500))
    }
    if (cameraActive.value && boxId.value) {
      await capturePhoto()
    }
  }
})

/// Watch for voice reject command
watch(() => voiceReject.value, async () => {
  if (voiceReject.value > 0 && uploadedItem.value) {
    console.log('Reject trigger from voice command')
    await rejectItem()
  }
})

// Watch for voice update commands
watch(() => voiceUpdateTrigger.value, () => {
  if (voiceUpdateTrigger.value > 0 && uploadedItem.value) {
    console.log('Update trigger from voice command')
    
    // Update description if provided
    if (voiceUpdateDescription.value) {
      console.log('Updating userDescription to:', voiceUpdateDescription.value)
      uploadedItem.value.userDescription = voiceUpdateDescription.value
    }
    
    // Update count if provided
    if (voiceUpdateCount.value !== null) {
      console.log('Updating count to:', voiceUpdateCount.value)
      uploadedItem.value.count = voiceUpdateCount.value
    }
  }
})

// Check if camera is supported
const isCameraSupported = computed(() => {
  return isSecureContext.value && hasCameraSupport.value
})

// Check camera support on mount
onMounted(async () => {
  // Check for box parameter from route (could be from voice navigation)
  const boxParam = route.query.box as string
  if (boxParam) {
    boxId.value = boxParam
  }
  
  // Check if we're in a secure context (HTTPS or localhost)
  isSecureContext.value = window.isSecureContext || false
  
  // Check if mediaDevices API is available
  hasCameraSupport.value = !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia)
  
  if (!isSecureContext.value) {
    console.warn('Not in secure context. Camera access requires HTTPS or localhost.')
  }
  
  if (!hasCameraSupport.value) {
    console.warn('getUserMedia API not supported in this browser.')
  }

  // Automatically start camera if supported
  if (isCameraSupported.value) {
    await startCamera()
  }
})

function goBack(): void {
  stopCamera()
  router.back()
}

async function startCamera(): Promise<void> {
  // Additional validation
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    error.value = 'Selaimesi ei tue kameran käyttöä. Varmista että käytät HTTPS-yhteyttä.'
    return
  }

  if (!window.isSecureContext) {
    error.value = 'Kamera vaatii turvallisen yhteyden (HTTPS). Käytä osoitetta https://localhost tai palvelinta HTTPS:llä.'
    return
  }

  try {
    error.value = ''
    mediaStream = await navigator.mediaDevices.getUserMedia({
      video: {
        facingMode: 'environment', // Use back camera on mobile
        width: { ideal: 1920 },
        height: { ideal: 1080 }
      }
    })
    
    // Set cameraActive first so the video element is rendered
    cameraActive.value = true
    
    // Wait for next tick to ensure video element is in DOM
    await nextTick()
    
    if (videoElement.value) {
      videoElement.value.srcObject = mediaStream
      
      // Wait for video to be ready and start playing
      try {
        await videoElement.value.play()
      } catch (playErr) {
        console.warn('Video play promise rejected:', playErr)
        // This is often fine - the video will autoplay anyway
      }
    } else {
      error.value = 'Kameran alustus epäonnistui'
      stopCamera()
    }
  } catch (err) {
    console.error('Camera error:', err)
    
    // Provide more specific error messages
    if (err instanceof Error) {
      if (err.name === 'NotAllowedError') {
        error.value = 'Kameran käyttö estetty. Anna selaimelle lupa käyttää kameraa.'
      } else if (err.name === 'NotFoundError') {
        error.value = 'Kameraa ei löytynyt. Varmista että laitteessasi on kamera.'
      } else if (err.name === 'NotReadableError') {
        error.value = 'Kamera on jo käytössä toisessa sovelluksessa.'
      } else if (err.name === 'OverconstrainedError') {
        error.value = 'Kameran asetukset eivät ole tuettuja.'
      } else if (err.name === 'SecurityError') {
        error.value = 'Kamera vaatii HTTPS-yhteyden. Käytä osoitetta https://localhost tai palvelinta HTTPS:llä.'
      } else {
        error.value = `Kameran käynnistys epäonnistui: ${err.message}`
      }
    } else {
      error.value = 'Kameran käynnistys epäonnistui'
    }
    cameraActive.value = false
  }
}

function stopCamera(): void {
  if (mediaStream) {
    mediaStream.getTracks().forEach(track => track.stop())
    mediaStream = null
  }
  cameraActive.value = false
}

async function capturePhoto(): Promise<void> {
  if (!boxId.value) {
    error.value = 'Syötä laatikon tunnus ennen kuvan ottamista'
    return
  }

  // If camera is not active, start it first
  if (!cameraActive.value) {
    // Clear any existing timeout
    if (previewTimeout !== null) {
      clearTimeout(previewTimeout)
      previewTimeout = null
    }
    
    // Clear preview
    previewImage.value = null
    
    // Start camera and wait for it to be ready
    await startCamera()
    await new Promise(resolve => setTimeout(resolve, 500))
  }

  if (!videoElement.value || !canvasElement.value) return

  // Clear any existing timeout
  if (previewTimeout !== null) {
    clearTimeout(previewTimeout)
    previewTimeout = null
  }

  const video = videoElement.value
  const canvas = canvasElement.value
  
  // Set canvas dimensions to match video
  canvas.width = video.videoWidth
  canvas.height = video.videoHeight
  
  // Draw the current video frame to canvas
  const context = canvas.getContext('2d')
  if (!context) return
  
  context.drawImage(video, 0, 0, canvas.width, canvas.height)
  
  // Convert canvas to blob
  canvas.toBlob(async (blob) => {
    if (!blob) {
      error.value = 'Kuvan tallentaminen epäonnistui'
      return
    }
    
    // Show preview
    previewImage.value = URL.createObjectURL(blob)
    
    // Stop camera temporarily
    stopCamera()
    
    // Upload image
    await uploadImage(blob)
    
    // After 5 seconds, clear preview and restart camera
    previewTimeout = window.setTimeout(async () => {
      previewImage.value = null
      if (isCameraSupported.value) {
        await startCamera()
      }
    }, 5000)
  }, 'image/jpeg', 0.9)
}

async function uploadImage(blob: Blob): Promise<void> {
  if (!boxId.value) {
    error.value = 'Syötä laatikon tunnus'
    return
  }

  try {
    uploading.value = true
    error.value = ''
    
    // Convert blob to File object
    const file = new File([blob], 'photo.jpg', { type: 'image/jpeg' })
    
    const result = await apiService.uploadImage(file, boxId.value)
    uploadedItem.value = result
  } catch (err) {
    error.value = `Lataus epäonnistui: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Upload error:', err)
  } finally {
    uploading.value = false
  }
}

async function saveItem(): Promise<void> {
  if (!uploadedItem.value) return

  try {
    saving.value = true
    error.value = ''
    
    const success = await apiService.saveItem(uploadedItem.value)
    if (success) {
      alert('Kohde tallennettu onnistuneesti!')
      // Clear the uploaded item data after successful save
      uploadedItem.value = null
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

async function rejectItem(): Promise<void> {
  if (!uploadedItem.value) return

  try {
    deleting.value = true
    error.value = ''
    
    const success = await apiService.deleteItem(uploadedItem.value.id)
    if (success) {
      // Only clear the uploaded item data, keep camera and preview as is
      uploadedItem.value = null
    } else {
      error.value = 'Hylätään epäonnistui'
    }
  } catch (err) {
    error.value = `Hylkääminen epäonnistui: ${err instanceof Error ? err.message : 'Tuntematon virhe'}`
    console.error('Delete error:', err)
  } finally {
    deleting.value = false
  }
}

// Clean up camera on component unmount
onBeforeUnmount(() => {
  if (previewTimeout !== null) {
    clearTimeout(previewTimeout)
  }
  stopCamera()
})
</script>

<style scoped>
/* Most common styles removed - now in global style.css */

.camera-video {
  width: 100%;
  max-height: 500px;
  border-radius: 8px;
  border: 1px solid #e0e0e0;
  background: #000;
  display: block;
}

.camera-controls {
  display: flex;
  gap: 10px;
  margin-top: 10px;
}

.btn-camera, .btn-capture {
  flex: 1;
  padding: 15px;
  background-color: #10b981;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 18px;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-camera:hover:not(:disabled), .btn-capture:hover:not(:disabled) {
  background-color: #059669;
}

.btn-camera:disabled, .btn-capture:disabled {
  background-color: #9ca3af;
  cursor: not-allowed;
}

.preview-section {
  margin: 20px 0;
}

.preview-image {
  width: 100%;
  max-height: 400px;
  object-fit: contain;
  border-radius: 8px;
  border: 1px solid #e0e0e0;
  margin-bottom: 10px;
}

.item-details {
  border-top: 2px solid #e0e0e0;
  padding-top: 30px;
}

.item-details h2 {
  color: #10b981;
  margin-bottom: 20px;
}
</style>
