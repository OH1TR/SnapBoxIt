import axios from 'axios';
import { useSettingsStore } from '../stores/settingsStore';

class ApiService {
  constructor() {
    this.settingsStore = null;
  }

  _getSettings() {
    if (!this.settingsStore) {
      this.settingsStore = useSettingsStore();
    }
    return this.settingsStore;
  }

  _getAxiosConfig() {
    const settings = this._getSettings();
    return {
      baseURL: settings.apiBaseUrl,
      headers: {
        'Authorization': settings.getAuthHeader()
      }
    };
  }

  async uploadImage(imageFile, boxId) {
    const formData = new FormData();
    formData.append('file', imageFile, 'image.jpg');

    const config = this._getAxiosConfig();
    const response = await axios.put(
      `/Image/upload/${encodeURIComponent(boxId)}`,
      formData,
      {
        ...config,
        headers: {
          ...config.headers,
          'Content-Type': 'multipart/form-data'
        }
      }
    );
    return response.data;
  }

  async searchItems(query, count = 10) {
    const config = this._getAxiosConfig();
    const response = await axios.post('/Data/FindItems', { Query: query, Count: count }, config);
    return response.data;
  }

  async saveItem(item) {
    const config = this._getAxiosConfig();
    const response = await axios.put(`/Data/Save/${encodeURIComponent(item.id)}`, item, config);
    return response.status === 200;
  }

  async deleteItem(itemId) {
    const config = this._getAxiosConfig();
    const response = await axios.delete(`/Data/${encodeURIComponent(itemId)}`, config);
    return response.status === 200;
  }

  async getImageUrl(blobId, thumbnail = false) {
    const settings = this._getSettings();
    const thumbParam = thumbnail ? '?thumb=true' : '';
    return `${settings.apiBaseUrl}Data/Image/${encodeURIComponent(blobId)}${thumbParam}`;
  }

  async getImageBytes(blobId, thumbnail = false) {
    const config = this._getAxiosConfig();
    const thumbParam = thumbnail ? '?thumb=true' : '';
    const response = await axios.get(
      `/Data/Image/${encodeURIComponent(blobId)}${thumbParam}`,
      {
        ...config,
        responseType: 'arraybuffer'
      }
    );
    return response.data;
  }

  async printLabel(type, text) {
    const config = this._getAxiosConfig();
    const response = await axios.post('/Data/PrintLabel', { Type: type, Text: text }, config);
    return response.status === 200;
  }

  async getBoxes() {
    const config = this._getAxiosConfig();
    const response = await axios.get('/Data/GetBoxes', config);
    return response.data;
  }

  async getBoxContents(boxId) {
    const config = this._getAxiosConfig();
    const response = await axios.get(`/Data/GetBoxContents/${encodeURIComponent(boxId)}`, config);
    return response.data;
  }
}

export default new ApiService();
