import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { InventoryItem } from '@/types'
import { ProductCard } from '@/components/ProductCard'
import type { useCart } from '@/hooks/useCart'
import { Button } from '@/components/ui/button'

const BRANDS = ['All', 'Bauer', 'CCM', 'True']
const CATEGORIES = ['All', 'Mask', 'Pads', 'Glove & Blocker', 'Chest & Arms']

export function ShopPage({ cart }: { cart: ReturnType<typeof useCart> }) {
  const [brand, setBrand] = useState('All')
  const [category, setCategory] = useState('All')

  const { data: items = [], isLoading, error } = useQuery({
    queryKey: ['items'],
    queryFn: api.getItems,
  })

  const filtered = items.filter((i: InventoryItem) =>
    (brand === 'All' || i.brand === brand) &&
    (category === 'All' || i.category === category)
  )

  if (isLoading) return <div className="text-center py-20 text-muted-foreground">Loading gear...</div>
  if (error) return <div className="text-center py-20 text-destructive">Failed to load products.</div>

  return (
    <div>
      <h1 className="text-2xl font-bold mb-4">Goalie Equipment</h1>

      <div className="flex flex-wrap gap-2 mb-6">
        <div className="flex gap-1">
          {BRANDS.map(b => (
            <Button key={b} size="sm" variant={brand === b ? 'default' : 'outline'} onClick={() => setBrand(b)}>{b}</Button>
          ))}
        </div>
        <div className="flex gap-1">
          {CATEGORIES.map(c => (
            <Button key={c} size="sm" variant={category === c ? 'default' : 'outline'} onClick={() => setCategory(c)}>{c}</Button>
          ))}
        </div>
      </div>

      {filtered.length === 0 ? (
        <p className="text-muted-foreground">No products match the selected filters.</p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {filtered.map((item: InventoryItem) => (
            <ProductCard key={item.id} item={item} onAddToCart={cart.addItem} />
          ))}
        </div>
      )}
    </div>
  )
}
