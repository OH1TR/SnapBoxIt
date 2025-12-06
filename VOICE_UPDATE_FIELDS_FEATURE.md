# Voice Command Feature: Update Description and Count

## Overview
This feature enables users to update the `userDescription` and `count` fields of an uploaded item using voice commands in the UploadPage.

## Voice Commands

### Update Description
When the user says:
- "päivitä kuvaus: [text]"
- "kuvaus: [text]"
- "lisää kuvaus: [text]"

The system will update the `userDescription` field with the provided text.

### Update Count
When the user says:
- "päivitä lukumäärä: [number]"
- "määrä: [number]"
- "lukumäärä: [number]"

The system will update the `count` field with the provided number.

## Implementation Details

### 1. New Event Types
Added to `inventoryRealtimeService.ts`:
```typescript
export interface UpdateItemFieldEvent {
  type: 'update_item_field'
  field: 'userDescription' | 'count'
  value: string | number
}
```

### 2. New Function Definitions
Added two new functions to the OpenAI Realtime API function list:

- **update_item_description**: Updates the userDescription field
- **update_item_count**: Updates the count field

### 3. Handler Functions
In `inventoryRealtimeService.ts`, added:
- `handleUpdateItemDescription(args)`: Processes description updates
- `handleUpdateItemCount(args)`: Processes count updates

Both functions emit `update_item_field` events to the UI.

### 4. VoiceController Updates
- Added `update-field` event to emits
- Imported `UpdateItemFieldEvent` type
- Added event listener for `update_item_field` events
- Implemented `handleUpdateField()` function to process and emit updates

### 5. App.vue Updates
- Added reactive refs for voice update data:
  - `voiceUpdateDescription`: Stores the description to update
  - `voiceUpdateCount`: Stores the count to update
  - `voiceUpdateTrigger`: Counter to trigger watchers
- Provided these refs to child components
- Added `handleUpdateField()` function to process update events

### 6. UploadPage.vue Updates
- Injected the voice update refs from App.vue
- Added a watcher on `voiceUpdateTrigger` that:
  - Checks if an uploaded item exists
  - Updates `uploadedItem.value.userDescription` if description was provided
  - Updates `uploadedItem.value.count` if count was provided

## How It Works

1. User speaks a command like "päivitä kuvaus: tämä on testi"
2. OpenAI's Realtime API transcribes the speech and recognizes the intent
3. The AI calls the `update_item_description` function with the description
4. `handleUpdateItemDescription()` emits an `update_item_field` event
5. VoiceController receives the event and emits `update-field` to App
6. App's `handleUpdateField()` stores the value and increments the trigger
7. UploadPage's watcher detects the trigger change
8. UploadPage updates the `uploadedItem` reactive object
9. The UI automatically updates to show the new values

## Requirements
- The user must be on the UploadPage
- An item must have been uploaded (uploadedItem must exist)
- Voice assistant must be active and connected

## Benefits
- Hands-free editing of item information
- Natural language interaction
- Immediate visual feedback in the UI
- Works seamlessly with existing save/reject functionality

## Example Usage

**User**: "ota kuva"
*[Camera captures and uploads image]*

**User**: "päivitä kuvaus: sininen työkalupakki, sisältää ruuvimeisseleitä"
*[userDescription field updates immediately]*

**User**: "päivitä lukumäärä: 3"
*[count field updates to 3]*

**User**: "tallenna"
*[Item is saved with all the voice-updated information]*
