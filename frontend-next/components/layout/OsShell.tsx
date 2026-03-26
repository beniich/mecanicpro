'use client'

import React, { ReactNode, useState, useEffect } from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { useNotificationHub } from '@/lib/hooks/useSignalR'

const NAV_GROUPS = [
  {
    label: 'ATELIER & TECHNIQUE',
    icon: 'precision_manufacturing',
    color: '#FF6B00',
    items: [
      { href: '/hub',        icon: 'grid_view',    label: 'Hub Opérationnel' },
      { href: '/planning',   icon: 'calendar_month', label: 'Planning Atelier' },
      { href: '/taches',     icon: 'task_alt',     label: 'Journal des Tâches' },
      { href: '/diagnostics', icon: 'biotech',     label: 'Diagnostic OBD' },
      { href: '/modules',    icon: 'map',          label: 'Mappage Visuel 3D', badge: 'NEW' },
    ],
  },
  {
    label: 'RELATION CLIENT',
    icon: 'groups',
    color: '#00eefc',
    items: [
      { href: '/crm',             icon: 'contacts',       label: 'Gestion Clients (CRM)' },
      { href: '/parc-vehicules',  icon: 'directions_car', label: 'Parc Véhicules' },
      { href: '/ventes',          icon: 'receipt',        label: 'Ventes & Devis' },
      { href: '/ecommerce',       icon: 'storefront',     label: 'E-Commerce' },
      { href: '/support-chat',    icon: 'support_agent',  label: 'Support Chat' },
    ],
  },
  {
    label: 'LOGISTIQUE & STOCK',
    icon: 'inventory_2',
    color: '#a78bfa',
    items: [
      { href: '/inventaire',         icon: 'inventory_2',    label: 'Inventaire Stock' },
      { href: '/catalogue',          icon: 'category',       label: 'Catalogue Pièces' },
      { href: '/commandes',          icon: 'local_shipping', label: 'Commandes Fournisseurs' },
      { href: '/suivi-expeditions',  icon: 'track_changes',  label: 'Suivi Expéditions' },
    ],
  },
  {
    label: 'FINANCE & RH',
    icon: 'account_balance',
    color: '#34d399',
    items: [
      { href: '/facturation',    icon: 'receipt_long',  label: 'Facturation' },
      { href: '/tresorerie',     icon: 'payments',      label: 'Trésorerie' },
      { href: '/gestion-rh',    icon: 'badge',         label: 'Gestion RH' },
      { href: '/planning-staff', icon: 'groups',        label: 'Planning Staff' },
    ],
  },
  {
    label: 'SYSTÈME',
    icon: 'terminal',
    color: '#94a3b8',
    items: [
      { href: '/explorateur', icon: 'terminal',         label: 'Explorateur de Modules' },
      { href: '/parametres',  icon: 'tune',             label: 'Paramètres' },
    ],
  },
]

import { useAuthStore } from '@/lib/store'
import { useRouter } from 'next/navigation'

export function OsShell({ children }: { children: ReactNode }) {
  const pathname = usePathname()
  const router = useRouter()
  const token = useAuthStore((state) => state.token)
  const user = useAuthStore((state) => state.user)
  const logout = useAuthStore((state) => state.logout)
  
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const [openGroups, setOpenGroups] = useState<string[]>(['ATELIER & TECHNIQUE'])

  const isActive = (href: string) =>
    pathname === href || (href === '/hub' && pathname === '/') || (pathname?.startsWith(href) && href !== '/')

  // Redirect to login if not authenticated
  useEffect(() => {
    if (!token && pathname !== '/login') {
      router.push('/login')
    }
  }, [token, pathname, router])

  // Auto-open the group containing the active page
  useEffect(() => {
    setIsMobileMenuOpen(false)
    for (const group of NAV_GROUPS) {
      if (group.items.some(item => isActive(item.href))) {
        setOpenGroups(prev => prev.includes(group.label) ? prev : [...prev, group.label])
        break
      }
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pathname])

  const { connection, notifications } = useNotificationHub()

  const toggleGroup = (label: string) => {
    setOpenGroups(prev =>
      prev.includes(label) ? prev.filter(g => g !== label) : [...prev, label]
    )
  }

  if (!token && pathname !== '/login') {
    return <div className="min-h-screen bg-[#0a0a0a]" /> // Blank during redirect
  }

  if (pathname === '/login') {
    return <>{children}</>
  }

  return (
    <div className="os-shell">
      {/* ── TOP BAR ── */}
      <header className="os-topbar">
        <div className="os-topbar-brand">
          <button 
            className="os-mobile-menu-trigger md:hidden"
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          >
            <span className="material-symbols-outlined">
              {isMobileMenuOpen ? 'close' : 'menu'}
            </span>
          </button>
          <span className="material-symbols-outlined os-brand-icon">speed</span>
          <span className="os-brand-text">MECAPRO OS V5.0</span>
        </div>
        <div className="os-topbar-right">
          {notifications.length > 0 && (
            <div className="bg-[#ff7351]/20 text-[#ff7351] px-3 py-1 mr-4 rounded font-mono text-[9px] uppercase tracking-widest border border-[#ff7351]/40 flex items-center gap-2 animate-pulse">
              <span className="material-symbols-outlined text-[12px]">notifications_active</span>
              {notifications[0]?.message || 'Nouvelle Alerte'} ({notifications.length})
            </div>
          )}

          <div className="os-link-status">
            <span className="os-ping-dot">
              <span className={`os-ping-ring ${connection ? 'animate-ping' : ''}`} style={{ borderColor: connection ? '#34d399' : '#ff7351' }} />
              <span className="os-ping-core" style={{ backgroundColor: connection ? '#34d399' : '#ff7351' }} />
            </span>
            <span className="os-link-label">{connection ? 'Link Established' : 'Offline'}</span>
          </div>
          <div className="os-topbar-user">
            <span className="text-[10px] font-headline font-bold text-[#00eefc] mr-2 hidden md:inline">{user?.name?.toUpperCase() || 'OPERATOR'}</span>
            <button 
              onClick={() => logout()}
              className="material-symbols-outlined text-xl text-neutral-600 hover:text-[#ff7351] transition-colors"
              title="Logout"
            >
              logout
            </button>
          </div>
        </div>
      </header>

      <div className="os-body">
        {/* ── SIDEBAR ── */}
        <aside className={`os-sidebar ${isMobileMenuOpen ? 'os-sidebar--open' : ''}`}>
          <nav className="os-nav os-nav--grouped">
            {NAV_GROUPS.map(group => {
              const isOpen = openGroups.includes(group.label)
              const hasActive = group.items.some(item => isActive(item.href))
              return (
                <div key={group.label} className="os-nav-group">
                  <button
                    className={`os-nav-group-header ${hasActive ? 'os-nav-group-header--active' : ''}`}
                    onClick={() => toggleGroup(group.label)}
                    style={{ '--group-color': group.color } as React.CSSProperties}
                  >
                    <span className="material-symbols-outlined os-nav-group-icon" style={{ color: group.color }}>{group.icon}</span>
                    <span className="os-nav-group-label">{group.label}</span>
                    <span className={`material-symbols-outlined os-nav-group-chevron ${isOpen ? 'rotate-180' : ''}`}>expand_more</span>
                  </button>
                  {isOpen && (
                    <div className="os-nav-group-items">
                      {group.items.map(item => {
                        const on = isActive(item.href)
                        return (
                          <Link
                            key={item.href}
                            href={item.href}
                            className={on ? 'os-nav-item os-nav-item--active os-nav-item--sub' : 'os-nav-item os-nav-item--sub'}
                          >
                            <span
                              className="material-symbols-outlined os-nav-icon"
                              style={{ fontVariationSettings: on ? "'FILL' 1" : "'FILL' 0", fontSize: '16px' }}
                            >
                              {item.icon}
                            </span>
                            <span className="os-nav-label">{item.label}</span>
                            {'badge' in item && item.badge && (
                              <span className="os-nav-badge">{item.badge}</span>
                            )}
                          </Link>
                        )
                      })}
                    </div>
                  )}
                </div>
              )
            })}
          </nav>

          {/* Core Telemetry */}
          <div className="os-telemetry">
            <p className="os-telemetry-label">CORE TELEMETRY</p>
            <div className="os-telemetry-bar-track">
              <div className="os-telemetry-bar-fill" />
            </div>
            <div className="os-telemetry-values">
              <span>42°C</span>
              <span>NOMINAL</span>
            </div>
          </div>
        </aside>


        {/* ── MAIN ── */}
        <main className="os-main">
          <div className="os-glow-orange" />
          <div className="os-glow-cyan" />
          <div className="os-content">
            {children}
          </div>
        </main>
      </div>

      {/* ── STATUS BAR ── */}
      <footer className="os-statusbar">
        <div className="os-statusbar-items">
          <div className="os-status-item">
            <span className="material-symbols-outlined os-status-icon os-status-icon--active">settings_ethernet</span>
            <span className="os-status-label os-status-label--active">C# API ACTIVE</span>
          </div>
          <div className="os-status-item">
            <span className="material-symbols-outlined os-status-icon">javascript</span>
            <span className="os-status-label">NODE.JS READY</span>
          </div>
          <div className="os-status-item">
            <span className="material-symbols-outlined os-status-icon">psychology</span>
            <span className="os-status-label">AI NOMINAL</span>
          </div>
        </div>
        <span className="os-build-label">MECAPRO_OS // BUILD_5.0.8842-X</span>
      </footer>
      {/* ── MOBILE OVERLAY ── */}
      {isMobileMenuOpen && (
        <div 
          className="os-mobile-overlay md:hidden" 
          onClick={() => setIsMobileMenuOpen(false)}
        />
      )}
    </div>
  )
}
