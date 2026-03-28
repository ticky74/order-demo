import { useState } from 'react'
import { Routes, Route } from 'react-router-dom'
import { NavBar } from '@/components/NavBar'
import { ShopPage } from '@/pages/ShopPage'
import { CheckoutPage } from '@/pages/CheckoutPage'
import { OrdersPage } from '@/pages/OrdersPage'
import { OrderDetailPage } from '@/pages/OrderDetailPage'
import { useCart } from '@/hooks/useCart'

export default function App() {
  const cart = useCart()
  const [, forceRender] = useState(0)

  return (
    <div className="min-h-screen bg-background">
      <NavBar
        cartCount={cart.items.reduce((n, i) => n + i.quantity, 0)}
        onUserChange={() => forceRender(n => n + 1)}
      />
      <main className="container mx-auto px-4 py-6">
        <Routes>
          <Route path="/" element={<ShopPage cart={cart} />} />
          <Route path="/checkout" element={<CheckoutPage cart={cart} />} />
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/orders/:id" element={<OrderDetailPage />} />
        </Routes>
      </main>
    </div>
  )
}
