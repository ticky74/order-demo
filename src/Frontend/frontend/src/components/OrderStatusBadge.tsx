import { Badge } from '@/components/ui/badge'

const variants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  Pending: 'secondary',
  Confirmed: 'default',
  Failed: 'destructive',
}

export function OrderStatusBadge({ status }: { status: string }) {
  return <Badge variant={variants[status] ?? 'outline'}>{status}</Badge>
}
