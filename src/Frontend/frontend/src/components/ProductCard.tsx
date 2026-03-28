import type { InventoryItem } from '@/types'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

export function ProductCard({
  item,
  onAddToCart,
}: {
  item: InventoryItem
  onAddToCart: (item: InventoryItem) => void
}) {
  const outOfStock = item.stockQty === 0

  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          <CardTitle className="text-base leading-tight">{item.name}</CardTitle>
          <Badge variant="outline" className="shrink-0">{item.brand}</Badge>
        </div>
      </CardHeader>
      <CardContent className="flex-1">
        <p className="text-sm text-muted-foreground">{item.description}</p>
        <p className="mt-2 text-xs text-muted-foreground">
          {outOfStock ? (
            <span className="text-destructive font-medium">Out of stock</span>
          ) : (
            <span>{item.stockQty} in stock</span>
          )}
        </p>
      </CardContent>
      <CardFooter className="flex items-center justify-between">
        <span className="font-semibold text-lg">${item.price.toFixed(2)}</span>
        <Button size="sm" disabled={outOfStock} onClick={() => onAddToCart(item)}>
          Add to Cart
        </Button>
      </CardFooter>
    </Card>
  )
}
