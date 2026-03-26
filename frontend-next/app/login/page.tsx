'use client'

import React, { useState } from 'react'
import { useRouter } from 'next/navigation'
import api from '@/lib/api'
import { useAuthStore } from '@/lib/store'
import { signIn, useSession } from 'next-auth/react'
import { useEffect } from 'react'

export default function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const router = useRouter()
  const setToken = useAuthStore((state) => state.setToken)
  const setUser = useAuthStore((state) => state.setUser)
  const { data: session, status } = useSession()

  // Sync NextAuth session with Zustand store
  useEffect(() => {
    if (session?.user) {
      setToken('google-session') // Placeholder token
      setUser({
        id: (session.user as any).id || session.user.email || 'google-user',
        name: session.user.name || session.user.email || 'Google User',
        email: session.user.email || '',
        role: 'User'
      })
      router.push('/hub')
    }
  }, [session, setToken, setUser, router])

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')

    try {
      // Call the Next.js internal API (BFF) which will set the HttpOnly cookie
      const res = await api.post('/api/internal/auth/login', { email, password })
      const data = res.data
      
      if (data.userId) {
        // No longer storing the token in Zustand/localStorage!
        setToken('secure-cookie') // Placeholder to trick old components until fully refactored
        setUser({
          id: data.userId,
          name: data.email,
          email: data.email,
          role: data.roles?.[0] || 'User'
        })
        router.push('/hub')
      } else if (data.requires2Fa) {
        setError('2FA required (not implemented in this UI yet)')
      }
    } catch (err: any) {
      setError(err.response?.data?.error || 'Invalid credentials or server unreachable')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-[#0a0a0a] flex items-center justify-center p-4 relative overflow-hidden">
      {/* Background Glows */}
      <div className="absolute top-1/4 -left-20 w-96 h-96 bg-[#FF6B00]/10 rounded-full blur-[120px]" />
      <div className="absolute bottom-1/4 -right-20 w-96 h-96 bg-[#00eefc]/10 rounded-full blur-[120px]" />
      
      <div className="max-w-md w-full relative z-10">
        <div className="text-center mb-10">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-white/5 border border-white/10 mb-6 shadow-2xl">
            <span className="material-symbols-outlined text-4xl text-[#FF6B00]">speed</span>
          </div>
          <h1 className="font-headline text-4xl font-black text-white tracking-tighter uppercase italic">
            MECAPRO <span className="text-[#FF6B00] not-italic">OS V5.0</span>
          </h1>
          <p className="text-neutral-500 text-xs uppercase tracking-[0.3em] font-mono mt-2">Industrial Management Interface</p>
        </div>

        <div className="glass-panel p-8 rounded-2xl border border-white/5 relative overflow-hidden">
          <div className="absolute top-0 left-0 w-full h-[2px] bg-gradient-to-r from-transparent via-[#FF6B00] to-transparent" />
          
          <form onSubmit={handleLogin} className="space-y-6">
            <div>
              <label className="block text-[10px] font-headline font-bold text-neutral-500 uppercase tracking-widest mb-2">Operator Identity (Email)</label>
              <input 
                type="email" 
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="w-full bg-white/5 border border-white/10 rounded-lg px-4 py-3 text-white text-sm focus:outline-none focus:border-[#FF6B00]/50 focus:bg-white/10 transition-all font-mono"
                placeholder="operator@mecapro.io"
              />
            </div>

            <div>
              <label className="block text-[10px] font-headline font-bold text-neutral-500 uppercase tracking-widest mb-2">Access Key (Password)</label>
              <input 
                type="password" 
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="w-full bg-white/5 border border-white/10 rounded-lg px-4 py-3 text-white text-sm focus:outline-none focus:border-[#FF6B00]/50 focus:bg-white/10 transition-all font-mono"
                placeholder="••••••••"
              />
            </div>

            {error && (
              <div className="bg-red-500/10 border border-red-500/20 text-red-500 text-[10px] uppercase font-bold tracking-widest p-3 rounded-lg text-center">
                Access Denied: {error}
              </div>
            )}

            <button 
              type="submit"
              disabled={loading}
              className="w-full bg-[#FF6B00] hover:bg-[#FF7A2F] text-black font-headline font-black py-4 rounded-xl transition-all shadow-[0_0_20px_rgba(255,107,0,0.3)] hover:shadow-[0_0_30px_rgba(255,107,0,0.5)] active:scale-[0.98] disabled:opacity-50 uppercase tracking-[0.1em]"
            >
              {loading ? 'Authenticating...' : 'Establish Connection'}
            </button>

            <div className="relative flex items-center justify-center py-2">
              <div className="flex-grow border-t border-white/5"></div>
              <span className="flex-shrink mx-4 text-[9px] text-neutral-600 font-mono tracking-widest uppercase">Or OAuth Interface</span>
              <div className="flex-grow border-t border-white/5"></div>
            </div>

            <button 
              type="button"
              onClick={() => signIn('google')}
              className="w-full bg-white/5 hover:bg-white/10 text-white border border-white/10 font-headline font-bold py-3 rounded-xl transition-all flex items-center justify-center gap-3 active:scale-[0.98]"
            >
              <svg className="w-4 h-4" viewBox="0 0 24 24">
                <path fill="currentColor" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                <path fill="currentColor" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                <path fill="currentColor" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" />
                <path fill="currentColor" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
              </svg>
              Login with Google Terminal
            </button>
          </form>
        </div>

        <p className="text-center mt-8 text-neutral-600 text-[9px] uppercase tracking-widest">
          Secure Terminal // Authorization Required
          <br />
          &copy; 2026 MecaPro Technologies
        </p>
      </div>
    </div>
  )
}
