import axios, { AxiosRequestConfig } from 'axios'
import { useSettingsStore } from '../stores/settingsStore'
import type { ItemDto, SearchRequest, PrintRequest } from '../types'

class ApiService {
  private settingsStore: ReturnType<typeof useSettingsStore> | null = null

  private _getSettings(): ReturnType<typeof useSettingsStore> {
    if (!this.settingsStore) {
      this.settingsStore = useSettingsStore()
    }
    return this.settingsStore
  }

  private _getAxiosConfig(): AxiosRequestConfig {
    const settings = this._getSettings()
    return {
      baseURL: settings.apiBaseUrl,
      headers: {
        'Authorization': settings.getAuthHeader()
      }
    }
  }

  async uploadImage(imageFile: File, boxId: string): Promise<ItemDto> {
    const formData = new FormData()
    formData.append('file', imageFile, 'image.jpg')

    const config = this._getAxiosConfig()
    const response = await axios.put<ItemDto>(
      `/Image/upload/${encodeURIComponent(boxId)}`,
      formData,
      {
        ...config,
        headers: {
          ...config.headers,
          'Content-Type': 'multipart/form-data'
        }
      }
    )
    return response.data
  }

  async searchItems(query: string, count: number = 10): Promise<ItemDto[]> {
    const config = this._getAxiosConfig()
    const searchRequest: SearchRequest = { Query: query, Count: count }
    const response = await axios.post<ItemDto[]>('/Data/FindItems', searchRequest, config)
    return response.data
  }

  async saveItem(item: ItemDto): Promise<boolean> {
    const config = this._getAxiosConfig()
    const response = await axios.put(`/Data/Save/${encodeURIComponent(item.id)}`, item, config)
    return response.status === 200
  }

  async deleteItem(itemId: string): Promise<boolean> {
    const config = this._getAxiosConfig()
    const response = await axios.delete(`/Data/${encodeURIComponent(itemId)}`, config)
    return response.status === 200
  }

  async getImageUrl(blobId: string, thumbnail: boolean = false): Promise<string> {
    const settings = this._getSettings()
    const thumbParam = thumbnail ? '?thumb=true' : ''
    return `${settings.apiBaseUrl}Data/Image/${encodeURIComponent(blobId)}${thumbParam}`
  }

  async getImageBytes(blobId: string, thumbnail: boolean = false): Promise<ArrayBuffer> {
    const config = this._getAxiosConfig()
    const thumbParam = thumbnail ? '?thumb=true' : ''
    const response = await axios.get<ArrayBuffer>(
      `/Data/Image/${encodeURIComponent(blobId)}${thumbParam}`,
      {
        ...config,
        responseType: 'arraybuffer'
      }
    )
    return response.data
  }

  async printLabel(type: string, text: string): Promise<boolean> {
    const config = this._getAxiosConfig()
    const printRequest: PrintRequest = { Type: type, Text: text }
    const response = await axios.post('/Data/PrintLabel', printRequest, config)
    return response.status === 200
  }

  async getBoxes(): Promise<string[]> {
    const config = this._getAxiosConfig()
    const response = await axios.get<string[]>('/Data/GetBoxes', config)
    return response.data
  }

  async getBoxContents(boxId: string): Promise<ItemDto[]> {
    const config = this._getAxiosConfig()
    const response = await axios.get<ItemDto[]>(`/Data/GetBoxContents/${encodeURIComponent(boxId)}`, config)
    return response.data
  }
}

export default new ApiService()
