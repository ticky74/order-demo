export interface InventoryItem {
  id: string
  brand: string
  category: string
  name: string
  description: string
  price: number
  stockQty: number
}

export interface OrderItem {
  itemId: string
  itemName: string
  brand: string
  quantity: number
  unitPrice: number
}

export interface CartItem extends InventoryItem {
  quantity: number
}

export interface OrderLine {
  id: string
  itemId: string
  itemName: string
  brand: string
  quantity: number
  unitPrice: number
}

export interface Order {
  id: string
  userId: string
  status: 'Pending' | 'Confirmed' | 'Failed'
  totalAmount: number
  placedAt: string
  confirmedAt?: string
  failureReason?: string
  lines?: OrderLine[]
}

export interface User {
  id: string
  name: string
}
