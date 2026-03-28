import { Link } from 'react-router-dom'
import { UserSwitcher } from './UserSwitcher'
import { ShoppingCart } from 'lucide-react'

export function NavBar({ cartCount, onUserChange }: {
  cartCount: number
  onUserChange: () => void
}) {
  return (
    <nav className="border-b bg-background px-6 py-3 flex items-center justify-between">
      <Link to="/" className="font-bold text-xl tracking-tight">
        ⛸ GoalieGear
      </Link>
      <div className="flex items-center gap-4">
        <Link to="/orders" className="text-sm text-muted-foreground hover:text-foreground">
          My Orders
        </Link>
        <Link to="/checkout" className="relative flex items-center gap-1 text-sm">
          <ShoppingCart className="h-5 w-5" />
          {cartCount > 0 && (
            <span className="absolute -top-2 -right-2 bg-primary text-primary-foreground text-xs rounded-full h-4 w-4 flex items-center justify-center">
              {cartCount}
            </span>
          )}
        </Link>
        <UserSwitcher onChange={onUserChange} />
      </div>
    </nav>
  )
}
