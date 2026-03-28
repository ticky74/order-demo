import type { InventoryItem, Order, OrderItem } from '@/types'

async function get<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) throw new Error(`GET ${url} → ${res.status}`)
  return res.json() as Promise<T>
}

async function post<T>(url: string, body: unknown): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) throw new Error(`POST ${url} → ${res.status}`)
  return res.json() as Promise<T>
}

export const api = {
  getItems: (): Promise<InventoryItem[]> =>
    get('/api/catalog/items'),

  getOrders: (userId: string): Promise<Order[]> =>
    get(`/api/catalog/orders?userId=${userId}`),

  getOrder: (orderId: string): Promise<Order> =>
    get(`/api/catalog/orders/${orderId}`),

  placeOrder: (userId: string, items: OrderItem[]): Promise<{ orderId: string }> =>
    post('/api/orders', { userId, items }),
}
