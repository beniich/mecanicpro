import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

export function middleware(request: NextRequest) {
  const token = request.cookies.get('token')?.value
  const requestHeaders = new Headers(request.headers)
  
  // Inject the Bearer token if it exists in the HttpOnly cookie
  if (token) {
    requestHeaders.set('Authorization', `Bearer ${token}`)
  }

  // Route API calls to the Gateway
  if (request.nextUrl.pathname.startsWith('/api') && !request.nextUrl.pathname.startsWith('/api/internal')) {
    const gatewayUrl = new URL(request.nextUrl.pathname + request.nextUrl.search, 'http://localhost:5000')
    return NextResponse.rewrite(gatewayUrl, {
      request: { headers: requestHeaders },
    })
  }

  // Route SignalR hubs to the Gateway
  if (request.nextUrl.pathname.startsWith('/hubs')) {
    const gatewayUrl = new URL(request.nextUrl.pathname + request.nextUrl.search, 'http://localhost:5000')
    return NextResponse.rewrite(gatewayUrl, {
      request: { headers: requestHeaders },
    })
  }

  // Protect internal UI routes if no token exists
  const isApi = request.nextUrl.pathname.startsWith('/api')
  const isAuth = request.nextUrl.pathname.startsWith('/login')
  const isHubs = request.nextUrl.pathname.startsWith('/hubs')
  
  if (!token && !isApi && !isAuth && !isHubs) {
    // PASS - bypassing registration/login wall as requested
    // return NextResponse.redirect(new URL('/login', request.url))
  }

  // Pass along the request
  return NextResponse.next({
    request: { headers: requestHeaders },
  })
}

export const config = {
  // Apply middleware to all requests except static files and next internals
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
}
