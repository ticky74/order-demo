import type { User } from '@/types'

export const USERS: User[] = [
  { id: '11111111-0000-0000-0000-000000000001', name: 'Mike Smith' },
  { id: '11111111-0000-0000-0000-000000000002', name: 'Sara Jones' },
  { id: '11111111-0000-0000-0000-000000000003', name: 'Dave Lee' },
]

const USER_KEY = 'orderdemo_user_id'

export function getCurrentUser(): User {
  const id = localStorage.getItem(USER_KEY) ?? USERS[0].id
  return USERS.find(u => u.id === id) ?? USERS[0]
}

export function setCurrentUser(userId: string): void {
  localStorage.setItem(USER_KEY, userId)
}
