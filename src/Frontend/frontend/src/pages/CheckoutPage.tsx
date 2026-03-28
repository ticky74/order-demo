import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import { getCurrentUser } from '@/lib/users'
import type { useCart } from '@/hooks/useCart'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'

export function CheckoutPage({ cart }: { cart: ReturnType<typeof useCart> }) {
  const navigate = useNavigate()
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [card, setCard] = useState({ name: '', number: '', expiry: '', cvv: '' })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (cart.items.length === 0) return

    setSubmitting(true)
    setError(null)

    try {
      const user = getCurrentUser()
      const orderItems = cart.items.map(i => ({
        itemId: i.id,
        itemName: i.name,
        brand: i.brand,
        quantity: i.quantity,
        unitPrice: i.price,
      }))

      const result = await api.placeOrder(user.id, orderItems)
      cart.clearCart()
      void navigate(`/orders/${result.orderId}`)
    } catch {
      setError('Failed to place order. Please try again.')
      setSubmitting(false)
    }
  }

  if (cart.items.length === 0) {
    return (
      <div className="text-center py-20">
        <p className="text-muted-foreground">Your cart is empty.</p>
        <Button className="mt-4" onClick={() => void navigate('/')}>Browse Gear</Button>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold">Checkout</h1>

      <Card>
        <CardHeader><CardTitle>Order Summary</CardTitle></CardHeader>
        <CardContent className="space-y-2">
          {cart.items.map(item => (
            <div key={item.id} className="flex justify-between text-sm">
              <span>{item.name} × {item.quantity}</span>
              <span>${(item.price * item.quantity).toFixed(2)}</span>
            </div>
          ))}
          <Separator />
          <div className="flex justify-between font-semibold">
            <span>Total</span>
            <span>${cart.total.toFixed(2)}</span>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Payment Details</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1">
              <Label>Cardholder Name</Label>
              <Input placeholder="Wayne Gretzky" value={card.name} onChange={e => setCard(c => ({ ...c, name: e.target.value }))} required />
            </div>
            <div className="space-y-1">
              <Label>Card Number (any 16 digits)</Label>
              <Input placeholder="4111 1111 1111 1111" value={card.number} onChange={e => setCard(c => ({ ...c, number: e.target.value }))} maxLength={19} required />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <Label>Expiry</Label>
                <Input placeholder="12/28" value={card.expiry} onChange={e => setCard(c => ({ ...c, expiry: e.target.value }))} required />
              </div>
              <div className="space-y-1">
                <Label>CVV</Label>
                <Input placeholder="123" value={card.cvv} onChange={e => setCard(c => ({ ...c, cvv: e.target.value }))} maxLength={4} required />
              </div>
            </div>
            {error && <p className="text-destructive text-sm">{error}</p>}
            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting ? 'Placing Order...' : `Place Order — $${cart.total.toFixed(2)}`}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
