import { useState } from 'react'
import type { CartItem, InventoryItem } from '@/types'

export function useCart() {
  const [items, setItems] = useState<CartItem[]>([])

  const addItem = (item: InventoryItem) => {
    setItems(prev => {
      const existing = prev.find(i => i.id === item.id)
      if (existing) {
        return prev.map(i => i.id === item.id ? { ...i, quantity: i.quantity + 1 } : i)
      }
      return [...prev, { ...item, quantity: 1 }]
    })
  }

  const removeItem = (itemId: string) =>
    setItems(prev => prev.filter(i => i.id !== itemId))

  const clearCart = () => setItems([])

  const total = items.reduce((sum, i) => sum + i.price * i.quantity, 0)

  return { items, addItem, removeItem, clearCart, total }
}
