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

          <div v-if="cameraActive" class="camera-container">
            <video
              ref="videoElement"
              autoplay
              playsinline
              class="camera-video"
            ></video>
            <div class="camera-controls">
              <button @click="capturePhoto" class="btn-capture" :disabled="!boxId">
                📷 Ota kuva
              </button>
              <button @click="stopCamera" class="btn-secondary">
                Sulje kamera
              </button>
            </div>
          </div>

          <canvas ref="canvasElement" style="display: none"></canvas>
        </div>

        <div v-if="previewImage" class="preview-section">
          <img :src="previewImage" alt="Preview" class="preview-image" />
          <button @click="retakePhoto" class="btn-secondary">
            🔄 Ota uusi kuva
          </button>
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

// Inject voice-controlled box selection and camera trigger
const voiceSelectedBoxId = inject<any>('voiceSelectedBoxId', ref(''))
const cameraTrigger = inject<any>('cameraTrigger', ref(0))
const voiceReject = inject<any>('voiceReject', ref(0))

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

// Watch for voice reject command
watch(() => voiceReject.value, async () => {
  if (voiceReject.value > 0 && uploadedItem.value) {
    console.log('Reject trigger from voice command')
    await rejectItem()
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

  if (!videoElement.value || !canvasElement.value) return

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
    
    // Stop camera
    stopCamera()
    
    // Upload image
    await uploadImage(blob)
  }, 'image/jpeg', 0.9)
}

function retakePhoto(): void {
  previewImage.value = null
  uploadedItem.value = null
  error.value = ''
  startCamera()
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
      resetForm()
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

async function rejectItem(): Promise<void> {
  if (!uploadedItem.value) return


  try {
    deleting.value = true
    error.value = ''
    
    const success = await apiService.deleteItem(uploadedItem.value.id)
    if (success) {
      resetForm()
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

function resetForm(): void {
  uploadedItem.value = null
  previewImage.value = null
  boxId.value = ''
  stopCamera()
}

// Clean up camera on component unmount
onBeforeUnmount(() => {
  stopCamera()
})
</script>

<style scoped>
.upload-page {
  max-width: 800px;
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

.upload-container {
  background: white;
  border-radius: 12px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.form-section {
  margin-bottom: 30px;
}

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

.btn-secondary {
  flex: 1;
  padding: 15px;
  background-color: #6b7280;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 18px;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-secondary:hover {
  background-color: #4b5563;
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

.detail-group {
  margin-bottom: 15px;
}

.detail-group label {
  display: block;
  font-weight: 500;
  color: #6b7280;
  font-size: 14px;
  margin-bottom: 5px;
}

.detail-group p {
  margin: 0;
  color: #2c3e50;
  font-size: 16px;
}

.button-group {
  display: flex;
  gap: 10px;
  margin-top: 20px;
}

.btn-primary, .btn-danger {
  flex: 1;
  padding: 12px 20px;
  border: none;
  border-radius: 6px;
  font-size: 16px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background-color: #0066cc;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: #0052a3;
}

.btn-danger {
  background-color: #dc2626;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background-color: #b91c1c;
}

.btn-primary:disabled, .btn-danger:disabled {
  background-color: #9ca3af;
  cursor: not-allowed;
}

.error-message {
  margin-top: 20px;
  padding: 12px;
  background-color: #fee;
  color: #c00;
  border: 1px solid #fcc;
  border-radius: 6px;
}

.warning-message {
  margin: 20px 0;
  padding: 16px;
  background-color: #fff3cd;
  color: #856404;
  border: 1px solid #ffeaa7;
  border-radius: 6px;
}

.warning-message p {
  margin: 8px 0;
}

.warning-message p:first-child {
  margin-top: 0;
}

.warning-message p:last-child {
  margin-bottom: 0;
}
</style>
