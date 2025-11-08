<template>
  <div class="main-page">
    <div class="header">
      <h1>SnapBox</h1>
      <p>J‰rjestelm‰ komponenttien hallintaan</p>
    </div>

    <div class="menu-grid">
      <button @click="navigateTo('/upload')" class="menu-button upload">
        <div class="icon">??</div>
        <h3>Lataa kuva</h3>
        <p>Ota kuva ja lataa se j‰rjestelm‰‰n</p>
      </button>

      <button @click="navigateTo('/search')" class="menu-button search">
        <div class="icon">??</div>
        <h3>Hae</h3>
        <p>Etsi komponentteja j‰rjestelm‰st‰</p>
      </button>

      <button @click="navigateTo('/boxes')" class="menu-button boxes">
        <div class="icon">??</div>
        <h3>Laatikot</h3>
        <p>Selaa laatikoiden sis‰ltˆ‰</p>
      </button>

      <button @click="navigateTo('/voice')" class="menu-button voice">
        <div class="icon">??</div>
        <h3>AI-Avustaja</h3>
        <p>Puhu tai kirjoita AI-avustajalle</p>
      </button>

      <button @click="navigateTo('/print')" class="menu-button print">
        <div class="icon">???</div>
        <h3>Tulosta</h3>
        <p>Tulosta tarra tai QR-koodi</p>
      </button>

      <button @click="navigateTo('/settings')" class="menu-button settings">
        <div class="icon">??</div>
        <h3>Asetukset</h3>
        <p>M‰‰rit‰ API-asetukset</p>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()

function navigateTo(path: string): void {
  router.push(path)
}
</script>

<style scoped>
.main-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.header {
  text-align: center;
  margin-bottom: 40px;
}

.header h1 {
  font-size: 2.5rem;
  color: #2c3e50;
  margin-bottom: 10px;
}

.header p {
  color: #666;
  font-size: 1.1rem;
}

.menu-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
  margin-top: 30px;
}

.menu-button {
  background: white;
  border: 2px solid #e0e0e0;
  border-radius: 12px;
  padding: 30px 20px;
  cursor: pointer;
  transition: all 0.3s ease;
  text-align: center;
}

.menu-button:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
  border-color: #0066cc;
}

.menu-button .icon {
  font-size: 3rem;
  margin-bottom: 15px;
}

.menu-button h3 {
  font-size: 1.3rem;
  color: #2c3e50;
  margin: 10px 0;
}

.menu-button p {
  color: #666;
  font-size: 0.9rem;
  margin: 0;
}

.menu-button.upload:hover { border-color: #10b981; }
.menu-button.search:hover { border-color: #3b82f6; }
.menu-button.boxes:hover { border-color: #f59e0b; }
.menu-button.voice:hover { border-color: #667eea; }
.menu-button.print:hover { border-color: #8b5cf6; }
.menu-button.settings:hover { border-color: #6b7280; }

@media (max-width: 768px) {
  .menu-grid {
    grid-template-columns: 1fr;
  }
}
</style>
