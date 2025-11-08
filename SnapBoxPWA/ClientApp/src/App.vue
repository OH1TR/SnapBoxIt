<template>
  <div id="app">
    <header v-if="$route.path === '/'">
      <h1>SnapBox PWA</h1>
    </header>
    <main>
      <router-view />
    </main>
  </div>
</template>

<script setup>
// No imports needed, router-view is globally available
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  background: #f5f5f5;
}

#app {
  min-height: 100vh;
}

header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 20px;
  text-align: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

header h1 {
  margin: 0;
  font-size: 2rem;
}

main {
  padding: 20px;
}
</style>
