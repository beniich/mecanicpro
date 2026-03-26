import { NextResponse } from 'next/server'

export async function POST(request: Request) {
  try {
    const body = await request.json()
    
    // Call the .NET API Gateway directly for authentication
    const response = await fetch('http://localhost:5000/api/v1/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    })

    const data = await response.json()

    if (!response.ok) {
      return NextResponse.json({ error: data.error || 'Authentication failed' }, { status: response.status })
    }

    if (data.accessToken) {
      // Create successful response avoiding passing the token directly to the frontend
      const res = NextResponse.json({
        userId: data.userId,
        email: data.email,
        roles: data.roles
      })
      
      // Securely store the token inside an HttpOnly cookie
      res.cookies.set({
        name: 'token',
        value: data.accessToken,
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        path: '/',
        maxAge: 60 * 60, // 1 hour matching the JWT backend Expiration
      })

      return res
    }
    
    return NextResponse.json(data)
  } catch (error) {
    return NextResponse.json({ error: 'BFF Gateway Connection Error' }, { status: 500 })
  }
}
