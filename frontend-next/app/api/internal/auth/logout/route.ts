import { NextResponse } from 'next/server'

export async function POST() {
  const res = NextResponse.json({ success: true })
  
  // Hard delete the token cookie on logout
  res.cookies.delete('token')
  return res
}
