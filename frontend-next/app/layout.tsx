import type { Metadata } from 'next'
import '@/styles/globals.css'
import { OsShell } from '@/components/layout/OsShell'

export const metadata: Metadata = {
  title: { default: 'MecaPro OS', template: '%s — MecaPro OS V5.0' },
  description: 'MecaPro OS V5.0 — Système de gestion garage automobile',
}

import { QueryProvider } from '@/components/providers/QueryProvider'

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="fr" className="dark">
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link
          href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300;400;500;600;700;900&family=Inter:wght@300;400;500;600;700&family=Rajdhani:wght@500;600;700&display=swap"
          rel="stylesheet"
        />
        <link
          href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap"
          rel="stylesheet"
        />
      </head>
      <body className="bg-[#0e0e0e] text-white font-['Inter']">
        <div className="crt-overlay" />
        <QueryProvider>
          <OsShell>{children}</OsShell>
        </QueryProvider>
      </body>
    </html>
  )
}
