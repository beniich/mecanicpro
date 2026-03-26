import axios from 'axios'

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'

const api = axios.create({
  baseURL: API_URL,
})

// We rely on Next.js Edge Middleware to attach the Bearer token directly
// from the HttpOnly cookie, so we don't need a request interceptor here.

// Interceptor for 401 errors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
        // Clear HttpOnly cookie via internal BFF endpoint
        try { await axios.post('/api/internal/auth/logout') } catch (e) {}
        
        // Let Next.js middleware handle the redirect or force it client-side
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export default api
