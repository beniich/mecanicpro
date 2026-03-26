import { create } from 'zustand'

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
  user: null,
  token: typeof window !== 'undefined' ? localStorage.getItem('token') : null,
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
    } catch (e) {}
    
    set({ user: null, token: null })
    if (typeof window !== 'undefined') {
      window.location.href = '/login'
    }
  },
}))
