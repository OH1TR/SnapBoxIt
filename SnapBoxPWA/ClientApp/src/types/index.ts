export interface ItemDto {
  id: string
  title?: string
  category?: string
  detailedDescription?: string
  colors?: string[]
  count?: number
  userDescription?: string
  blobId?: string
  boxId?: string
  imageUrl?: string
  createdAt?: string
  updatedAt?: string
}

export interface SearchRequest {
  Query: string
  Count: number
}

export interface PrintRequest {
  Type: string
  Text: string
}
