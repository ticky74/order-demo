import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api'
import { getCurrentUser } from '@/lib/users'
import type { Order } from '@/types'
import { OrderStatusBadge } from '@/components/OrderStatusBadge'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'

export function OrdersPage() {
  const user = getCurrentUser()

  const { data: orders = [], isLoading } = useQuery<Order[]>({
    queryKey: ['orders', user.id],
    queryFn: () => api.getOrders(user.id),
    refetchInterval: 5000,
  })

  if (isLoading) return <div className="text-center py-20 text-muted-foreground">Loading orders...</div>

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">My Orders — {user.name}</h1>
        <Link to="/"><Button variant="outline" size="sm">Continue Shopping</Button></Link>
      </div>

      {orders.length === 0 ? (
        <p className="text-muted-foreground text-center py-20">No orders yet.</p>
      ) : (
        <div className="space-y-3">
          {orders.map((order: Order) => (
            <Link key={order.id} to={`/orders/${order.id}`}>
              <Card className="hover:bg-muted/50 transition-colors cursor-pointer">
                <CardContent className="flex items-center justify-between py-4">
                  <div>
                    <p className="font-medium text-sm font-mono">{order.id}</p>
                    <p className="text-xs text-muted-foreground">
                      {new Date(order.placedAt).toLocaleString()}
                    </p>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className="font-semibold">${order.totalAmount.toFixed(2)}</span>
                    <OrderStatusBadge status={order.status} />
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
