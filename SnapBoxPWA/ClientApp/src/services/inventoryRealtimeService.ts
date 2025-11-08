/**
 * Inventory Realtime Service with Function Calling
 * 
 * Extends the base RealtimeService to add inventory-specific
 * function calling capabilities for voice-controlled operations.
 */

import { RealtimeService } from './realtimeService'
import apiService from './apiService'
import type { ItemDto } from '../types'

// Navigation event type
export interface NavigationEvent {
  type: 'navigate'
  route: string
  params?: {
    boxId?: string
    query?: string
  }
}

// Camera control event type
export interface CameraControlEvent {
  type: 'camera'
  action: 'capture' | 'start' | 'stop'
  boxId?: string
}

// Box selection event type
export interface BoxSelectionEvent {
  type: 'select_box'
  boxId: string
}

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
        case 'navigate_to_search':
          await this.handleNavigateToSearch(args)
          break
        case 'navigate_to_box_view':
          await this.handleNavigateToBoxView(args)
          break
        case 'navigate_to_upload':
          await this.handleNavigateToUpload(args)
          break
        case 'capture_photo':
          await this.handleCapturePhoto(args)
          break
        case 'select_box':
          await this.handleSelectBox(args)
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
   * Handle navigate to search function call
   */
  private async handleNavigateToSearch(args: string): Promise<void> {
    try {
      const { query } = JSON.parse(args)
      console.log('Navigating to search with query:', query)
      
      // Emit navigation event
      this.emit('navigate', {
        type: 'navigate',
        route: '/search',
        params: { query }
      } as NavigationEvent)

      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        message: 'Navigating to search page',
        query
      })
    } catch (error: any) {
      console.error('Error navigating to search:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle navigate to box view function call
   */
  private async handleNavigateToBoxView(args: string): Promise<void> {
    try {
      const { boxId } = JSON.parse(args)
      console.log('Navigating to box view with boxId:', boxId)
      
      // Emit navigation event
      this.emit('navigate', {
        type: 'navigate',
        route: '/boxes',
        params: { boxId }
      } as NavigationEvent)

      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        message: 'Navigating to box view page',
        boxId
      })
    } catch (error: any) {
      console.error('Error navigating to box view:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle navigate to upload page function call
   */
  private async handleNavigateToUpload(args: string): Promise<void> {
    try {
      const params = args ? JSON.parse(args) : {}
      const { boxId } = params
      console.log('Navigating to upload page, boxId:', boxId)
      
      // Emit navigation event
      this.emit('navigate', {
        type: 'navigate',
        route: '/upload',
        params: boxId ? { boxId } : undefined
      } as NavigationEvent)

      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        message: 'Navigating to upload page',
        boxId
      })
    } catch (error: any) {
      console.error('Error navigating to upload:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle capture photo function call
   */
  private async handleCapturePhoto(args: string): Promise<void> {
    try {
      const params = args ? JSON.parse(args) : {}
      const { boxId } = params
      console.log('Capture photo requested, boxId:', boxId)
      
      // Emit camera control event
      this.emit('camera', {
        type: 'camera',
        action: 'capture',
        boxId
      } as CameraControlEvent)

      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        message: boxId ? 'Capturing photo' : 'Box ID required before capturing photo',
        requiresBoxId: !boxId
      })
    } catch (error: any) {
      console.error('Error capturing photo:', error)
      this.sendFunctionResult(this.currentCallId!, {
        success: false,
        error: error.message
      })
    }
  }

  /**
   * Handle select box function call
   */
  private async handleSelectBox(args: string): Promise<void> {
    try {
      const { boxId } = JSON.parse(args)
      console.log('Selecting box:', boxId)
      
      // Emit box selection event
      this.emit('select_box', {
        type: 'select_box',
        boxId
      } as BoxSelectionEvent)

      this.sendFunctionResult(this.currentCallId!, {
        success: true,
        message: 'Box selected',
        boxId
      })
    } catch (error: any) {
      console.error('Error selecting box:', error)
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
      instructions: `Olet tekoälyavustaja SnapBox-varastojärjestelmälle.
Voit auttaa käyttäjiä:
- Etsimään tavaroita varastosta
- Näyttämään laatikon sisällön
- Näyttämään kaikki laatikot
- Lisäämään uusia kohteita
- Ottamaan kuvia
- Valitsemaan laatikoita
- Päivittämään tietojen tietoja
- Poistamaan tavaroita

TÄRKEÄÄ - NAVIGOINTI:
- Kun käyttäjä kysyy "missä on X" tai "etsi X", käytä ENSIN search_items-funktiota löytääksesi tavaran.
  SITTEN käytä navigate_to_search-funktiota näkyäksesi tulokset hakunäkymässä.
- Kun käyttäjä kysyy "näytä laatikko X" tai "mitä laatikossa X on", käytä navigate_to_box_view-funktiota.
- Kun käyttäjä sanoo "etsi" tai "hae", navigoi hakunäkymään käyttämällä navigate_to_search-funktiota.

TÄRKEÄÄ - KUVAAMINEN:
- Kun käyttäjä sanoo "lisää uusi", "uusi kohde", "lisää kuva" tai "lataa kuva", käytä navigate_to_upload-funktiota.
- Kun käyttäjä sanoo "ota kuva":
  * Jos boxId on tiedossa, käytä capture_photo-funktiota.
  * Jos boxId ei ole tiedossa, kerro että laatikko pitää valita ensin ja käytä select_box-funktiota.
- Kun käyttäjä sanoo "valitse laatikko X" tai "vaihda laatikko X", käytä select_box-funktiota.
- select_box-funktiota voi käyttää AINA kun käyttäjä haluaa vaihtaa laatikkoa, vaikka laatikko olisi jo valittu.

Pidä vastauksesi lyhyinä ja keskustelunomaisina, koska tämä on puhekäyttöliittymä.
Vastaa aina suomeksi.`,
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
    description: 'Etsi tavaroita varastosta hakusanalla. Käytä tätä kun käyttäjä haluaa löytää, etsiä tai paikantaa tavaroita.',
    parameters: {
      type: 'object',
      properties: {
        query: {
          type: 'string',
          description: 'Hakusana tavaroiden etsimiseen (esim. "ruuvimeisseli", "kaapelit", "keittiötavarat")'
        },
        count: {
          type: 'number',
          description: 'Maksimimäärä tuloksia (oletus: 10)',
          default: 10
        }
      },
      required: ['query']
    }
  },
  {
    type: 'function',
    name: 'get_box_contents',
    description: 'Hae kaikki tietyssä laatikossa olevat tavarat. Käytä tätä kun käyttäjä kysyy mitä tietyssä laatikossa on.',
    parameters: {
      type: 'object',
      properties: {
        boxId: {
          type: 'string',
          description: 'Laatikon tunniste, jonka sisältö haetaan (esim. "BOX-001")'
        }
      },
      required: ['boxId']
    }
  },
  {
    type: 'function',
    name: 'get_all_boxes',
    description: 'Hae lista kaikista laatikoista varastossa. Käytä tätä kun käyttäjä kysyy kaikista laatikoista tai haluaa nähdä mitä laatikoita on olemassa.',
    parameters: {
      type: 'object',
      properties: {}
    }
  },
  {
    type: 'function',
    name: 'navigate_to_search',
    description: 'Navigoi hakunäkymään ja näytä hakutulokset siellä. Käytä AINA kun käyttäjä kysyy tavaran sijaintia tai haluaa etsiä jotain. Kutsu tätä SEARCH_ITEMS-funktion JÄLKEEN näyttääksesi tulokset käyttäjälle.',
    parameters: {
      type: 'object',
      properties: {
        query: {
          type: 'string',
          description: 'Hakusana, joka näytetään hakunäkymässä'
        }
      },
      required: ['query']
    }
  },
  {
    type: 'function',
    name: 'navigate_to_box_view',
    description: 'Navigoi laatikkonäkymään ja näytä tietyn laatikon sisältö. Käytä kun käyttäjä haluaa nähdä mitä tietyssä laatikossa on.',
    parameters: {
      type: 'object',
      properties: {
        boxId: {
          type: 'string',
          description: 'Laatikon tunniste (esim. "BOX-001")'
        }
      },
      required: ['boxId']
    }
  },
  {
    type: 'function',
    name: 'navigate_to_upload',
    description: 'Navigoi lataussivulle uuden kohteen lisäämiseksi. Käytä kun käyttäjä sanoo "lisää uusi", "uusi kohde", "lisää kuva", "lataa kuva" tai haluaa lisätä jotain varastoon.',
    parameters: {
      type: 'object',
      properties: {
        boxId: {
          type: 'string',
          description: 'Valinnaisesti laatikon tunniste, johon kohde lisätään'
        }
      }
    }
  },
  {
    type: 'function',
    name: 'capture_photo',
    description: 'Ota kuva kameralla. TÄRKEÄÄ: Tarkista ENSIN että boxId on tiedossa. Jos ei ole, pyydä käyttäjää valitsemaan laatikko ensin käyttämällä select_box-funktiota.',
    parameters: {
      type: 'object',
      properties: {
        boxId: {
          type: 'string',
          description: 'Laatikon tunniste, johon kuva liitetään. PAKOLLINEN ennen kuvan ottamista.'
        }
      }
    }
  },
  {
    type: 'function',
    name: 'select_box',
    description: 'Valitse tai vaihda laatikko. Käytä kun käyttäjä sanoo "valitse laatikko X", "vaihda laatikko X" tai haluaa vaihtaa aktiivista laatikkoa. Toimii aina, vaikka laatikko olisi jo valittuna.',
    parameters: {
      type: 'object',
      properties: {
        boxId: {
          type: 'string',
          description: 'Valittavan laatikon tunniste (esim. "BOX-001")'
        }
      },
      required: ['boxId']
    }
  },
  {
    type: 'function',
    name: 'save_item',
    description: 'Tallenna tai päivitä tavaran tiedot varastoon. Käytä tätä kun käyttäjä haluaa muuttaa tavaran tietoja.',
    parameters: {
      type: 'object',
      properties: {
        id: {
          type: 'string',
          description: 'Tavaran tunniste'
        },
        boxId: {
          type: 'string',
          description: 'Laatikon tunniste, jossa tavara on'
        },
        title: {
          type: 'string',
          description: 'Tavaran otsikko'
        },
        category: {
          type: 'string',
          description: 'Tavaran kategoria'
        },
        detailedDescription: {
          type: 'string',
          description: 'Yksityiskohtainen kuvaus tavarasta'
        }
      },
      required: ['id', 'boxId']
    }
  },
  {
    type: 'function',
    name: 'delete_item',
    description: 'Poista tavara varastosta. Varmista AINA käyttäjältä ennen tämän funktion kutsumista.',
    parameters: {
      type: 'object',
      properties: {
        itemId: {
          type: 'string',
          description: 'Poistettavan tavaran tunniste'
        }
      },
      required: ['itemId']
    }
  }
]

export default new InventoryRealtimeService()
