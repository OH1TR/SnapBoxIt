# Quick Reference: Global CSS Classes

## Page Layout
```css
.page              /* max-width: 1200px, standard padding */
.page-narrow       /* max-width: 600px */
.page-medium       /* max-width: 800px */
.container         /* max-width: 1200px, centered */
```

## Headers & Navigation
```css
.header            /* flex layout with gap for page headers */
.back-button       /* standard back button style */
```

## Containers
```css
.white-container   /* white background, rounded, with shadow */
```

## Form Elements
```css
.form-group        /* form field container with spacing */
.input-field       /* text input styling */
.textarea-field    /* textarea styling */
.radio-group       /* radio button group container */
.radio-label       /* radio button label */
.readonly-field    /* read-only field display */
```

## Buttons
```css
.btn-primary       /* blue action button */
.btn-secondary     /* gray secondary button */
.btn-danger        /* red destructive button */
.button-group      /* flex container for multiple buttons */
.btn-cancel        /* dialog cancel button */
.btn-confirm       /* dialog confirm button */
```

## Messages & Feedback
```css
.error-message     /* red error message */
.success-message   /* green success message */
.warning-message   /* yellow warning message */
.info-message      /* blue info message */
```

## Loading States
```css
.loading           /* loading text display */
.loading-container /* loading container */
.spinner           /* spinning loader animation */
```

## Dialogs
```css
.dialog-overlay    /* modal background overlay */
.dialog-content    /* modal content container */
.dialog-actions    /* modal button container */
```

## Grids & Cards
```css
.menu-grid         /* auto-fit grid for menu items */
.results-grid      /* auto-fill grid for search results */
.card              /* generic card component */
```

## Other
```css
.divider           /* horizontal divider line */
.detail-group      /* detail label/value grouping */
```

## Usage Examples

### Standard Page Layout
```vue
<template>
  <div class="page">
    <div class="header">
      <button @click="goBack" class="back-button">? Back</button>
      <h1>Page Title</h1>
    </div>
    
    <div class="white-container">
      <!-- Page content -->
    </div>
  </div>
</template>
```

### Form Layout
```vue
<template>
  <div class="white-container">
    <div class="form-group">
      <label for="name">Name</label>
      <input id="name" v-model="name" class="input-field" />
    </div>
    
    <div class="form-group">
      <label for="description">Description</label>
      <textarea id="description" v-model="desc" class="textarea-field"></textarea>
    </div>
    
    <div class="button-group">
      <button @click="cancel" class="btn-secondary">Cancel</button>
      <button @click="save" class="btn-primary">Save</button>
    </div>
  </div>
</template>
```

### Dialog/Modal
```vue
<template>
  <div v-if="showDialog" class="dialog-overlay" @click="close">
    <div class="dialog-content" @click.stop>
      <h3>Confirm Action</h3>
      <p>Are you sure you want to continue?</p>
      <div class="dialog-actions">
        <button @click="close" class="btn-cancel">Cancel</button>
        <button @click="confirm" class="btn-confirm">Confirm</button>
      </div>
    </div>
  </div>
</template>
```

### Messages
```vue
<template>
  <div>
    <div v-if="error" class="error-message">{{ error }}</div>
    <div v-if="success" class="success-message">{{ success }}</div>
    <div v-if="warning" class="warning-message">{{ warning }}</div>
  </div>
</template>
```

### Grid Layouts
```vue
<template>
  <!-- Menu items -->
  <div class="menu-grid">
    <button class="card">Item 1</button>
    <button class="card">Item 2</button>
  </div>
  
  <!-- Search results -->
  <div class="results-grid">
    <div class="result-card">Result 1</div>
    <div class="result-card">Result 2</div>
  </div>
</template>
```
