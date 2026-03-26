import { create } from 'zustand'
import { signOut } from 'next-auth/react'

interface User {
  id: string
  name: string
  email: string
  role: string
}

interface AuthState {
  user: User | null
  token: string | null
  setToken: (token: string | null) => void
  setUser: (user: User | null) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  user: {
    id: '00000000-0000-0000-0000-000000000000',
    name: 'Operateur Test',
    email: 'test@mecapro.io',
    role: 'Admin'
  },
  token: typeof window !== 'undefined' ? (localStorage.getItem('token') || 'dev-token') : 'dev-token',
  setToken: (token) => {
    if (token) {
      localStorage.setItem('token', token)
    } else {
      localStorage.removeItem('token')
    }
    set({ token })
  },
  setUser: (user) => set({ user }),
  logout: async () => {
    try {
      await fetch('/api/internal/auth/logout', { method: 'POST' })
      // Also sign out from NextAuth (Google)
      await signOut({ redirect: false })
    } catch (e) {}
    
    set({ user: null, token: null })
    localStorage.removeItem('token')
    if (typeof window !== 'undefined') {
      window.location.href = '/login'
    }
  },
}))
