import { USERS, getCurrentUser, setCurrentUser } from '@/lib/users'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

export function UserSwitcher({ onChange }: { onChange: () => void }) {
  const current = getCurrentUser()

  return (
    <Select
      value={current.id}
      onValueChange={id => { setCurrentUser(id); onChange() }}
    >
      <SelectTrigger className="w-40">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {USERS.map(u => (
          <SelectItem key={u.id} value={u.id}>{u.name}</SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
