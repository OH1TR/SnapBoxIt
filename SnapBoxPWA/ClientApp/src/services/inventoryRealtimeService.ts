/**
 * Inventory Realtime Service with Function Calling
 * 
 * Extends the base RealtimeService to add inventory-specific
 * function calling capabilities for voice-controlled operations.
 */

import { RealtimeService } from './realtimeService'
import apiService from './apiService'
import type { ItemDto } from '../types'

export class InventoryRealtimeService extends RealtimeService {
  private currentCallId: string | null = null

  /**
   * Wait for the data channel to be open
   */
  private async waitForDataChannel(timeout = 5000): Promise<void> {
    const startTime = Date.now()
    
    while (!this.dataChannel || this.dataChannel.readyState !== 'open') {
      if (Date.now() - startTime > timeout) {
        throw new Error('Timeout waiting for data channel to open')
      }
      await new Promise(resolve => setTimeout(resolve, 100))
    }
  }

  /**
   * Set up function calling for inventory operations
   */
  setupInventoryFunctions(): void {
    console.log('Setting up inventory functions...')
    
    // Listen to ALL events to debug
    this.on('*', (event: any) => {
      console.log('ALL EVENTS:', event)
    })

    // Register function call handler
    this.on('response.function_call_arguments.done', async (event: any) => {
      console.log('Function call received:', event)
      const { call_id, name, arguments: args } = event

      this.currentCallId = call_id

      switch (name) {
        case 'search_items':
          await this.handleSearchItems(args)
          break
        case 'get_box_contents':
          await this.handleGetBoxContents(args)
          break
        case 'save_item':
          await this.handleSaveItem(args)
          break
        case 'delete_item':
          await this.handleDeleteItem(args)
          break
        case 'get_all_boxes':
          await this.handleGetAllBoxes()
          break
        default:
          console.warn('Unknown function call:', name)
          this.sendFunctionResult(call_id, {
            success: false,
            error: `Unknown function: ${name}`
          })
      }
    })
    
    console.log('Inventory functions setup complete')
  }

  /**
   * Handle search items function call
   */
  private async handleSearchItems(args: string): Promise<void> {
    try {
      const { query, count = 10 } = JSON.parse(args)
      console.log('Searching items:', query, count)
      
      const results = await apiService.searchItems(query, count)

      // Send results back to the assistant
      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        itemCount: results.length,
        items: results.map(item => ({
          id: item.id,
          boxId: item.boxId,
          title: item.title,
          category: item.category,
          detailedDescription: item.detailedDescription
        }))
      })
    } catch (error: any) {
      console.error('Error searching items:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle get box contents function call
   */
  private async handleGetBoxContents(args: string): Promise<void> {
    try {
      const { boxId } = JSON.parse(args)
      console.log('Getting box contents:', boxId)
      
      const items = await apiService.getBoxContents(boxId)

      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        boxId,
        itemCount: items.length,
        items: items.map(item => ({
          id: item.id,
          title: item.title,
          category: item.category,
          detailedDescription: item.detailedDescription
        }))
      })
    } catch (error: any) {
      console.error('Error getting box contents:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle get all boxes function call
   */
  private async handleGetAllBoxes(): Promise<void> {
    try {
      console.log('Getting all boxes')
      
      const boxes = await apiService.getBoxes()

      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        boxCount: boxes.length,
        boxes: boxes
      })
    } catch (error: any) {
      console.error('Error getting boxes:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle save item function call
   */
  private async handleSaveItem(args: string): Promise<void> {
    try {
      const itemData = JSON.parse(args) as ItemDto
      console.log('Saving item:', itemData)
      
      const success = await apiService.saveItem(itemData)

      this.sendFunctionResult(this.currentCallId!, {
        success,
        message: success ? 'Item saved successfully' : 'Failed to save item',
        itemId: itemData.id
      })
    } catch (error: any) {
      console.error('Error saving item:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle delete item function call
   */
  private async handleDeleteItem(args: string): Promise<void> {
    try {
      const { itemId } = JSON.parse(args)
      console.log('Deleting item:', itemId)
      
      const success = await apiService.deleteItem(itemId)

      this.sendFunctionResult(this.currentCallId!, {
        success,
        message: success ? 'Item deleted successfully' : 'Failed to delete item',
        itemId
      })
    } catch (error: any) {
      console.error('Error deleting item:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Send function call result back to the assistant
   */
  protected sendFunctionResult(callId: string, result: any): void {
    const event = {
      type: 'conversation.item.create',
      item: {
        type: 'function_call_output',
        call_id: callId,
        output: JSON.stringify(result)
      }
    }

    console.log('Sending function result:', event)
    this.sendEvent(event)
    
    // Trigger response to continue the conversation
    this.sendEvent({ type: 'response.create' })
  }

  /**
   * Connect with inventory function definitions
   */
  async connectWithFunctions(): Promise<void> {
    console.log('connectWithFunctions: Starting...')
    console.log('connectWithFunctions: Tools to register:', INVENTORY_FUNCTIONS)
    
    // First create session with function definitions
    await this.createSession({
      instructions: `You are an AI assistant for the SnapBox inventory management system.
You can help users:
- Search for items in their inventory
- View contents of specific boxes
- Get a list of all boxes
- Update item information
- Delete items

When searching, always use the search_items function to find items by description or name.
When asked about a specific box, use get_box_contents to see what's inside.
When asked to list all boxes, use get_all_boxes.
Always be helpful and confirm actions before making changes to the inventory.
Keep your responses concise and conversational since this is a voice interface.`,
      tools: INVENTORY_FUNCTIONS
    })

    console.log('connectWithFunctions: Session created')

    // Set up function handlers BEFORE connecting
    this.setupInventoryFunctions()

    console.log('connectWithFunctions: Function handlers set up')

    // Connect to OpenAI
    await this.connect()
    
    console.log('connectWithFunctions: Connected to OpenAI')
    
    // Wait for data channel to be ready
    await this.waitForDataChannel()
    
    console.log('connectWithFunctions: Data channel is open')
    
    // Send session.update to ensure tools are active
    this.sendEvent({
      type: 'session.update',
      session: {
        tools: INVENTORY_FUNCTIONS
      }
    })
    
    console.log('connectWithFunctions: Sent session.update with tools')
  }
}

// Function definitions for OpenAI Realtime API
export const INVENTORY_FUNCTIONS = [
  {
    type: 'function',
    name: 'search_items',
    description: 'Search for items in the inventory by query string. Use this when the user asks to find, search, or locate items.',
    parameters: {
      type: 'object',
      properties: {
        query: {
          type: 'string',
          description: 'The search query to find items (e.g., "screwdriver", "cables", "kitchen items")'
        },
        count: {
          type: 'number',
          description: 'Maximum number of results to return (default: 10)',
          default: 10
        }
      },
      required: ['query']
    }
  },
  {
    type: 'function',
    name: 'get_box_contents',
    description: 'Get all items stored in a specific box. Use this when the user asks what is in a particular box.',
    parameters: {
      type: 'object',
      properties: {
        boxId: {
          type: 'string',
          description: 'The ID of the box to retrieve contents from (e.g., "BOX-001")'
        }
      },
      required: ['boxId']
    }
  },
  {
    type: 'function',
    name: 'get_all_boxes',
    description: 'Get a list of all boxes in the inventory. Use this when the user asks about all boxes or wants to see what boxes exist.',
    parameters: {
      type: 'object',
      properties: {}
    }
  },
  {
    type: 'function',
    name: 'save_item',
    description: 'Save or update an item in the inventory. Use this when the user wants to modify item details.',
    parameters: {
      type: 'object',
      properties: {
        id: {
          type: 'string',
          description: 'The item ID'
        },
        boxId: {
          type: 'string',
          description: 'The box ID where the item is stored'
        },
        title: {
          type: 'string',
          description: 'Title of the item'
        },
        category: {
          type: 'string',
          description: 'Category of the item'
        },
        detailedDescription: {
          type: 'string',
          description: 'Detailed description of the item'
        }
      },
      required: ['id', 'boxId']
    }
  },
  {
    type: 'function',
    name: 'delete_item',
    description: 'Delete an item from the inventory. Always confirm with the user before calling this function.',
    parameters: {
      type: 'object',
      properties: {
        itemId: {
          type: 'string',
          description: 'The ID of the item to delete'
        }
      },
      required: ['itemId']
    }
  }
]

export default new InventoryRealtimeService()
