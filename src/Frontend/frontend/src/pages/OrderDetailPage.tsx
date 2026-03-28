import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { Order } from '@/types'
import { OrderStatusBadge } from '@/components/OrderStatusBadge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>()

  const { data: order, isLoading } = useQuery<Order>({
    queryKey: ['order', id],
    queryFn: () => api.getOrder(id!),
    refetchInterval: (query) => query.state.data?.status === 'Pending' ? 3000 : false,
  })

  if (isLoading) return <div className="text-center py-20 text-muted-foreground">Loading order...</div>
  if (!order) return <div className="text-center py-20 text-destructive">Order not found.</div>

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center gap-4">
        <Link to="/orders"><Button variant="outline" size="sm">← My Orders</Button></Link>
        <h1 className="text-2xl font-bold">Order Detail</h1>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base font-mono">{order.id}</CardTitle>
            <OrderStatusBadge status={order.status} />
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="text-sm text-muted-foreground space-y-1">
            <p>Placed: {new Date(order.placedAt).toLocaleString()}</p>
            {order.confirmedAt && <p>Confirmed: {new Date(order.confirmedAt).toLocaleString()}</p>}
            {order.failureReason && (
              <p className="text-destructive">Reason: {order.failureReason}</p>
            )}
          </div>

          <Separator />

          <div className="space-y-2">
            {order.lines?.map(line => (
              <div key={line.id} className="flex justify-between text-sm">
                <span>
                  <span className="font-medium">{line.itemName}</span>
                  <span className="text-muted-foreground"> × {line.quantity}</span>
                </span>
                <span>${(line.unitPrice * line.quantity).toFixed(2)}</span>
              </div>
            ))}
          </div>

          <Separator />

          <div className="flex justify-between font-semibold">
            <span>Total</span>
            <span>${order.totalAmount.toFixed(2)}</span>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
