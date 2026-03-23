'use client'
// ============================================================
// components/services/ServiceCard.tsx + ServicesMenu.tsx
// ============================================================

import { useState, useMemo } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { SERVICE_ICONS } from './ServiceIcons'
import { SERVICES, CATEGORY_LABELS, type Service, type ServiceCategory, type ServiceStatus } from './services.data'

// ─── Status config ────────────────────────────────────────────
const STATUS: Record<ServiceStatus, { label: string; dot: string; badge: string }> = {
  available: {
    label: 'Disponible',
    dot: 'bg-green-500',
    badge: 'bg-green-50 text-green-800 dark:bg-green-950 dark:text-green-300 border border-green-200 dark:border-green-800',
  },
  busy: {
    label: 'Occupé',
    dot: 'bg-amber-400 animate-pulse',
    badge: 'bg-amber-50 text-amber-800 dark:bg-amber-950 dark:text-amber-300 border border-amber-200 dark:border-amber-800',
  },
  unavailable: {
    label: 'Indisponible',
    dot: 'bg-gray-300',
    badge: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400 border border-gray-200 dark:border-gray-700',
  },
}

// ─── ServiceCard ──────────────────────────────────────────────
interface ServiceCardProps {
  service: Service
  isSelected?: boolean
  onSelect?: (id: string) => void
  variant?: 'grid' | 'compact' | 'featured'
}

export function ServiceCard({ service, isSelected, onSelect, variant = 'grid' }: ServiceCardProps) {
  const Icon = SERVICE_ICONS[service.iconKey]
  const st = STATUS[service.status]

  // Compact sidebar variant
  if (variant === 'compact') {
    return (
      <button
        onClick={() => onSelect?.(service.id)}
        className={[
          'w-full flex items-center gap-3 px-3 py-2.5 rounded-xl border text-left transition-all',
          isSelected
            ? 'border-amber-400 bg-amber-50 dark:bg-amber-950/30'
            : 'border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 hover:border-gray-300 dark:hover:border-gray-700',
        ].join(' ')}
      >
        <div className="shrink-0 w-9 h-9 rounded-lg bg-gray-50 dark:bg-gray-800 flex items-center justify-center overflow-hidden">
          {Icon && <Icon size={28} />}
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium text-gray-900 dark:text-white leading-tight truncate">{service.label}</div>
          <div className="text-xs text-gray-400 truncate">{service.sublabel}</div>
        </div>
        <div className={['w-2 h-2 rounded-full shrink-0', st.dot].join(' ')} />
      </button>
    )
  }

  // Featured large card variant
  if (variant === 'featured') {
    return (
      <Link
        href={service.href}
        className={[
          'relative flex flex-col p-5 rounded-2xl border overflow-hidden transition-all group',
          isSelected
            ? 'border-amber-400 bg-amber-50 dark:bg-amber-950/20 shadow-sm'
            : 'border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 hover:border-amber-300 dark:hover:border-amber-700 hover:shadow-md',
        ].join(' ')}
      >
        {service.activeCount !== undefined && (
          <span className="absolute top-3 left-3 w-6 h-6 rounded-full bg-amber-500 text-white text-xs font-bold flex items-center justify-center">
            {service.activeCount}
          </span>
        )}
        <span className={['absolute top-3 right-3 inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium', st.badge].join(' ')}>
          <span className={['w-1.5 h-1.5 rounded-full', st.dot].join(' ')} />{st.label}
        </span>

        <div className="mb-3 mt-2 w-14 h-14 rounded-xl bg-gray-50 dark:bg-gray-800 flex items-center justify-center overflow-hidden">
          {Icon && <Icon size={52} />}
        </div>
        <div className="text-sm font-semibold text-gray-900 dark:text-white mb-0.5">{service.label}</div>
        <div className="text-xs text-gray-500 dark:text-gray-400 mb-3">{service.sublabel}</div>
        <div className="text-xs text-gray-500 dark:text-gray-400 leading-relaxed line-clamp-2 flex-1">{service.description}</div>

        <div className="mt-3 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-gray-400">
          {service.estimatedTime && <span>⏱ {service.estimatedTime}</span>}
          {service.priceFrom !== undefined && (
            <span>💶 {service.priceFrom === 0 ? 'Gratuit' : `dès ${service.priceFrom} €`}</span>
          )}
          {service.mechanics !== undefined && <span>👤 {service.mechanics} tech.</span>}
        </div>
      </Link>
    )
  }

  // Default grid icon card
  return (
    <button
      onClick={() => onSelect?.(service.id)}
      className={[
        'group flex flex-col items-center gap-2 px-2 py-4 rounded-xl border text-center transition-all',
        isSelected
          ? 'border-amber-400 bg-amber-50 dark:bg-amber-950/30 shadow-sm'
          : 'border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 hover:border-amber-300 dark:hover:border-amber-700 hover:shadow-sm',
      ].join(' ')}
    >
      <div className={[
        'w-14 h-14 rounded-xl flex items-center justify-center transition-colors overflow-hidden',
        isSelected ? 'bg-amber-100 dark:bg-amber-900/30' : 'bg-gray-50 dark:bg-gray-800',
      ].join(' ')}>
        {Icon && <Icon size={52} />}
      </div>
      <div className="text-xs font-semibold text-gray-800 dark:text-gray-200 leading-snug px-1">{service.label}</div>
      <div className="flex items-center gap-1">
        <span className={['w-1.5 h-1.5 rounded-full', st.dot].join(' ')} />
        <span className="text-xs text-gray-400">{st.label}</span>
      </div>
    </button>
  )
}

// ─── ServicesMenu ─────────────────────────────────────────────
const CATS: Array<ServiceCategory | 'all'> = ['all', 'reception', 'reparation', 'depannage', 'gestion', 'client']

interface ServicesMenuProps {
  variant?: 'grid' | 'compact'
  showFilter?: boolean
  onServiceSelect?: (id: string) => void
  selectedServiceId?: string
  className?: string
}

export function ServicesMenu({
  variant = 'grid', showFilter = true, onServiceSelect,
  selectedServiceId, className = '',
}: ServicesMenuProps) {
  const router = useRouter()
  const [cat, setCat] = useState<ServiceCategory | 'all'>('all')
  const [search, setSearch] = useState('')

  const filtered = useMemo(() => {
    let list = [...SERVICES]
    if (cat !== 'all') list = list.filter(s => s.category === cat)
    if (search.trim()) {
      const q = search.toLowerCase()
      list = list.filter(s => `${s.label} ${s.sublabel} ${s.description}`.toLowerCase().includes(q))
    }
    return list
  }, [cat, search])

  const handleSelect = (id: string) => {
    if (onServiceSelect) { onServiceSelect(id); return }
    const svc = SERVICES.find(s => s.id === id)
    if (svc) router.push(svc.href)
  }

  if (variant === 'compact') {
    return (
      <div className={className}>
        {showFilter && (
          <input type="text" value={search} onChange={e => setSearch(e.target.value)}
            placeholder="Chercher un service…"
            className="w-full mb-3 text-sm border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:border-amber-400"
          />
        )}
        <div className="space-y-1.5">
          {filtered.map(s => (
            <ServiceCard key={s.id} service={s} variant="compact"
              isSelected={s.id === selectedServiceId} onSelect={handleSelect} />
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className={className}>
      {showFilter && (
        <div className="flex flex-wrap gap-2 mb-5 items-center">
          <div className="flex gap-2 flex-wrap flex-1">
            {CATS.map(c => (
              <button key={c} onClick={() => setCat(c)}
                className={[
                  'px-3 py-1.5 rounded-full text-xs font-medium border transition-all',
                  cat === c
                    ? 'bg-amber-500 border-amber-500 text-white'
                    : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 bg-white dark:bg-gray-900 hover:border-gray-300',
                ].join(' ')}>
                {c === 'all' ? 'Tous les services' : CATEGORY_LABELS[c as ServiceCategory]}
              </button>
            ))}
          </div>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm pointer-events-none">⌕</span>
            <input type="text" value={search} onChange={e => setSearch(e.target.value)}
              placeholder="Rechercher…"
              className="pl-8 pr-3 py-1.5 text-sm border border-gray-200 dark:border-gray-700 rounded-full bg-white dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:border-amber-400 w-36"
            />
          </div>
        </div>
      )}

      <div className="grid grid-cols-5 gap-3">
        {filtered.map(s => (
          <ServiceCard key={s.id} service={s} isSelected={s.id === selectedServiceId} onSelect={handleSelect} />
        ))}
        {filtered.length === 0 && (
          <div className="col-span-5 py-12 text-center text-sm text-gray-400">Aucun service trouvé</div>
        )}
      </div>

      <div className="mt-4 flex items-center justify-between text-xs text-gray-400 border-t border-gray-100 dark:border-gray-800 pt-3">
        <span>{filtered.length} service(s)</span>
        <div className="flex items-center gap-4">
          <span className="flex items-center gap-1.5"><span className="w-2 h-2 rounded-full bg-green-500" />Disponible</span>
          <span className="flex items-center gap-1.5"><span className="w-2 h-2 rounded-full bg-amber-400 animate-pulse" />Occupé</span>
          <span className="flex items-center gap-1.5"><span className="w-2 h-2 rounded-full bg-gray-300" />Indisponible</span>
        </div>
      </div>
    </div>
  )
}
