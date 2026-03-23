'use client'
// app/(app)/atelier/page.tsx — Page globale atelier MecaPro
import { useState } from 'react'
import Link from 'next/link'
import { ServicesMenu } from '@/components/services/ServicesMenu'
import { ServiceCard } from '@/components/services/ServicesMenu'
import { SERVICES } from '@/components/services/services.data'

type BayStatus = 'working' | 'available' | 'break'
type VehicleStatus = 'in_progress' | 'waiting_parts' | 'waiting_customer' | 'ready'
type Section = 'overview' | 'services' | 'bays' | 'vehicles'

interface Bay { id: string; name: string; initials: string; status: BayStatus; currentTask?: string; vehiclePlate?: string; progress?: number; timeLeft?: string }
interface ActiveVehicle { id: string; plate: string; make: string; model: string; service: string; mechanic: string; startTime: string; estimatedEnd: string; progress: number; status: VehicleStatus }
interface StockAlert { part: string; ref: string; stock: number; min: number }

const BAYS: Bay[] = [
  { id: 'b1', name: 'Marc Durand', initials: 'MD', status: 'working', currentTask: 'Révision freins AV', vehiclePlate: 'MN-012-OP', progress: 65, timeLeft: '45 min' },
  { id: 'b2', name: 'Jean-Luc Martin', initials: 'JM', status: 'working', currentTask: 'Vidange + filtres', vehiclePlate: 'AB-123-CD', progress: 80, timeLeft: '20 min' },
  { id: 'b3', name: 'Sophie Renard', initials: 'SR', status: 'working', currentTask: 'Diagnostic OBD P0301', vehiclePlate: 'EF-456-GH', progress: 40, timeLeft: '1h 10' },
  { id: 'b4', name: 'Pierre Leclerc', initials: 'PL', status: 'available' },
  { id: 'b5', name: 'Alain Dubois', initials: 'AD', status: 'break', currentTask: 'Pause déjeuner' },
]

const VEHICLES: ActiveVehicle[] = [
  { id: 'v1', plate: 'MN-012-OP', make: 'Volkswagen', model: 'Golf VII', service: 'Freins AV complets', mechanic: 'Marc D.', startTime: '09:30', estimatedEnd: '12:15', progress: 65, status: 'in_progress' },
  { id: 'v2', plate: 'AB-123-CD', make: 'Peugeot', model: '308 GT', service: 'Vidange + filtres', mechanic: 'Jean-Luc M.', startTime: '10:00', estimatedEnd: '11:30', progress: 80, status: 'in_progress' },
  { id: 'v3', plate: 'EF-456-GH', make: 'Renault', model: 'Clio V', service: 'Diagnostic + Bobine', mechanic: 'Sophie R.', startTime: '10:30', estimatedEnd: '13:00', progress: 40, status: 'waiting_parts' },
  { id: 'v4', plate: 'IJ-789-KL', make: 'BMW', model: 'Série 3', service: 'Distribution complète', mechanic: 'Pierre L.', startTime: '13:00', estimatedEnd: '18:00', progress: 0, status: 'waiting_customer' },
  { id: 'v5', plate: 'PQ-345-RS', make: 'Toyota', model: 'Yaris', service: 'Pneumatiques x4', mechanic: 'Alain D.', startTime: '08:30', estimatedEnd: '10:00', progress: 100, status: 'ready' },
]

const ALERTS: StockAlert[] = [
  { part: 'Plaquettes Bosch AV', ref: 'BRK-PLA-001', stock: 2, min: 5 },
  { part: 'Bobine Valeo', ref: 'IGN-COL-114', stock: 1, min: 3 },
  { part: 'Filtre huile Mann', ref: 'OIL-FLT-220', stock: 3, min: 8 },
]

const BAY_CFG: Record<BayStatus, { label: string; dot: string; border: string }> = {
  working: { label: 'En travail', dot: 'bg-amber-400 animate-pulse', border: 'border-amber-200 dark:border-amber-800' },
  available: { label: 'Disponible', dot: 'bg-green-500', border: 'border-green-200 dark:border-green-800' },
  break: { label: 'En pause', dot: 'bg-gray-300', border: 'border-gray-200 dark:border-gray-700' },
}

const VEH_CFG: Record<VehicleStatus, { label: string; color: string; bar: string; badge: string }> = {
  in_progress: { label: 'En cours', color: 'text-amber-700 dark:text-amber-400', bar: 'bg-amber-500', badge: 'bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800' },
  waiting_parts: { label: 'Attente pièce', color: 'text-blue-700 dark:text-blue-400', bar: 'bg-blue-500', badge: 'bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800' },
  waiting_customer: { label: 'Attente client', color: 'text-purple-700 dark:text-purple-400', bar: 'bg-purple-500', badge: 'bg-purple-50 dark:bg-purple-950/30 border border-purple-200 dark:border-purple-800' },
  ready: { label: '✓ Prêt', color: 'text-green-700 dark:text-green-400', bar: 'bg-green-500', badge: 'bg-green-50 dark:bg-green-950/30 border border-green-200 dark:border-green-800' },
}

function Kpi({ icon, value, label, sub, color }: { icon: string; value: string | number; label: string; sub?: string; color?: string }) {
  return (
    <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl p-4">
      <div className="flex items-start gap-3">
        <div className="w-9 h-9 rounded-lg bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-lg shrink-0">{icon}</div>
        <div>
          <div className={['text-2xl font-bold leading-none', color ?? 'text-gray-900 dark:text-white'].join(' ')}>{value}</div>
          <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{label}</div>
          {sub && <div className="text-xs text-gray-400">{sub}</div>}
        </div>
      </div>
    </div>
  )
}

export default function AtelierPage() {
  const [section, setSection] = useState<Section>('overview')
  const [selectedService, setSelectedService] = useState<string | null>(null)
  const selSvc = SERVICES.find(s => s.id === selectedService)

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950">

      {/* HEADER */}
      <div className="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 px-6 py-5">
        <div className="max-w-screen-2xl mx-auto">
          <div className="flex items-start justify-between gap-4 mb-5">
            <div className="flex items-center gap-3">
              <div className="w-11 h-11 rounded-xl bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center text-2xl">🏭</div>
              <div>
                <h1 className="text-xl font-bold text-gray-900 dark:text-white">Atelier MecaPro</h1>
                <div className="flex items-center gap-2 mt-0.5">
                  <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
                  <span className="text-xs text-green-700 dark:text-green-400 font-medium">Atelier ouvert</span>
                  <span className="text-xs text-gray-400 ml-1">· {new Date().toLocaleDateString('fr-FR',{weekday:'long',day:'numeric',month:'long'})}</span>
                </div>
              </div>
            </div>
            <div className="flex gap-2">
              <Link href="/vehicles/new" className="px-4 py-2 text-sm bg-amber-500 hover:bg-amber-600 text-white rounded-lg font-medium transition">
                + Nouveau véhicule
              </Link>
            </div>
          </div>

          {/* Nav tabs */}
          <div className="flex gap-1 bg-gray-100 dark:bg-gray-800 rounded-xl p-1 w-fit">
            {([['overview','📊 Vue d\'ensemble'],['services','🗂 Services'],['bays','👷 Postes'],['vehicles','🚗 Véhicules']] as [Section,string][]).map(([id,label]) => (
              <button key={id} onClick={() => setSection(id)}
                className={['px-4 py-2 text-sm font-medium rounded-lg transition-all',
                  section === id ? 'bg-white dark:bg-gray-900 text-gray-900 dark:text-white shadow-sm' : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'].join(' ')}>
                {label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="max-w-screen-2xl mx-auto px-6 py-6 space-y-6">

        {/* ── OVERVIEW ──────────────────────────────────────── */}
        {section === 'overview' && (<>
          <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4">
            <Kpi icon="🚗" value={VEHICLES.length} label="Véhicules en atelier" color="text-amber-600" />
            <Kpi icon="👷" value={BAYS.filter(b=>b.status==='working').length} label="Mécaniciens actifs" sub={`${BAYS.filter(b=>b.status==='available').length} disponibles`} color="text-green-600" />
            <Kpi icon="✅" value={VEHICLES.filter(v=>v.status==='ready').length} label="Prêts à livrer" color="text-green-600" />
            <Kpi icon="⚙️" value={VEHICLES.filter(v=>v.status==='waiting_parts').length} label="Attente pièce" color="text-blue-600" />
            <Kpi icon="📦" value={ALERTS.length} label="Alertes stock" color="text-red-600" />
            <Kpi icon="💶" value="4 280 €" label="CA du jour" sub="+12% vs hier" color="text-green-600" />
          </div>

          <div className="grid grid-cols-3 gap-6">
            {/* Services menu */}
            <div className="col-span-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl overflow-hidden">
              <div className="px-5 py-4 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
                <h2 className="text-sm font-semibold text-gray-900 dark:text-white">🗂 Services disponibles</h2>
                <button onClick={() => setSection('services')} className="text-xs text-gray-400 hover:text-amber-500 transition">Voir tout →</button>
              </div>
              <div className="p-5">
                <ServicesMenu showFilter={false} onServiceSelect={setSelectedService} selectedServiceId={selectedService ?? undefined} />
              </div>
            </div>

            {/* Right panel */}
            <div className="space-y-4">
              {/* Service detail */}
              {selSvc ? (
                <div className="bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800 rounded-2xl p-4">
                  <div className="text-xs font-medium text-amber-700 dark:text-amber-400 mb-2">Service sélectionné</div>
                  <div className="font-semibold text-gray-900 dark:text-white mb-1">{selSvc.label}</div>
                  <div className="text-xs text-gray-500 dark:text-gray-400 leading-relaxed mb-3">{selSvc.description}</div>
                  <div className="flex gap-2 text-xs text-gray-500 mb-3 flex-wrap">
                    {selSvc.estimatedTime && <span>⏱ {selSvc.estimatedTime}</span>}
                    {selSvc.priceFrom !== undefined && <span>💶 {selSvc.priceFrom===0?'Gratuit':`dès ${selSvc.priceFrom} €`}</span>}
                  </div>
                  <div className="flex gap-2">
                    <Link href={selSvc.href} className="flex-1 py-2 text-xs text-center bg-amber-500 hover:bg-amber-600 text-white rounded-lg font-medium transition">Accéder →</Link>
                    <button onClick={() => setSelectedService(null)} className="px-3 py-2 text-xs border border-amber-200 dark:border-amber-800 rounded-lg text-amber-700 dark:text-amber-400 hover:bg-amber-100 dark:hover:bg-amber-900/30 transition">✕</button>
                  </div>
                </div>
              ) : (
                <div className="bg-gray-50 dark:bg-gray-800/50 border border-dashed border-gray-300 dark:border-gray-700 rounded-2xl p-6 text-center">
                  <div className="text-3xl mb-2">👆</div>
                  <div className="text-sm text-gray-500 dark:text-gray-400">Cliquez sur un service pour voir le détail</div>
                </div>
              )}

              {/* Bays quick view */}
              <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl overflow-hidden">
                <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
                  <h3 className="text-sm font-semibold text-gray-900 dark:text-white">👷 Postes</h3>
                  <button onClick={() => setSection('bays')} className="text-xs text-gray-400 hover:text-amber-500 transition">Tous →</button>
                </div>
                <div className="p-3 space-y-2">
                  {BAYS.slice(0,4).map(bay => {
                    const c = BAY_CFG[bay.status]
                    return (
                      <div key={bay.id} className={['flex items-start gap-3 p-2.5 rounded-xl border bg-white dark:bg-gray-900', c.border].join(' ')}>
                        <div className="w-8 h-8 rounded-full bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center text-xs font-bold text-amber-700 dark:text-amber-400 shrink-0">{bay.initials}</div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center justify-between">
                            <span className="text-xs font-medium text-gray-900 dark:text-white truncate">{bay.name}</span>
                            <div className="flex items-center gap-1 shrink-0 ml-2">
                              <span className={['w-1.5 h-1.5 rounded-full', c.dot].join(' ')} />
                              <span className="text-xs text-gray-400">{c.label}</span>
                            </div>
                          </div>
                          {bay.vehiclePlate && (
                            <div className="flex items-center gap-2 mt-1">
                              <span className="font-mono text-xs bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400 px-1.5 py-0.5 rounded">{bay.vehiclePlate}</span>
                              <span className="text-xs text-gray-400 truncate">{bay.currentTask}</span>
                            </div>
                          )}
                          {bay.progress !== undefined && (
                            <div className="mt-1.5 flex items-center gap-2">
                              <div className="flex-1 h-1 bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
                                <div className="h-full bg-amber-500 rounded-full" style={{width:`${bay.progress}%`}} />
                              </div>
                              <span className="text-xs text-gray-400 shrink-0">{bay.timeLeft}</span>
                            </div>
                          )}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>

              {/* Stock alerts */}
              <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl overflow-hidden">
                <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
                  <h3 className="text-sm font-semibold text-gray-900 dark:text-white">📦 Alertes stock</h3>
                  <Link href="/parts?filter=lowstock" className="text-xs text-gray-400 hover:text-amber-500 transition">Gérer →</Link>
                </div>
                <div className="p-3 space-y-2">
                  {ALERTS.map((a, i) => (
                    <div key={i} className="flex items-center gap-3 p-2.5 bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-800 rounded-xl">
                      <span className="w-1.5 h-1.5 rounded-full bg-red-500 animate-pulse shrink-0" />
                      <div className="flex-1 min-w-0">
                        <div className="text-xs font-medium text-gray-900 dark:text-white truncate">{a.part}</div>
                        <div className="text-xs font-mono text-gray-400">{a.ref}</div>
                      </div>
                      <div className="text-xs font-bold text-red-700 dark:text-red-400 shrink-0">{a.stock}/{a.min}</div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Vehicles table */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl overflow-hidden">
            <div className="px-5 py-4 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-gray-900 dark:text-white">🚗 Véhicules en atelier — {VEHICLES.length} en cours</h2>
              <button onClick={() => setSection('vehicles')} className="text-xs text-gray-400 hover:text-amber-500 transition">Voir tout →</button>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-100 dark:border-gray-800">
                    {['Immat.','Véhicule','Service','Mécanicien','Horaires','Avancement','Statut',''].map(h => (
                      <th key={h} className="text-left text-xs font-medium text-gray-400 uppercase tracking-wider px-4 py-3">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {VEHICLES.map(v => {
                    const c = VEH_CFG[v.status]
                    return (
                      <tr key={v.id} className="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition-colors">
                        <td className="px-4 py-3"><span className="font-mono text-xs bg-amber-50 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 px-2 py-0.5 rounded border border-amber-200 dark:border-amber-800">{v.plate}</span></td>
                        <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">{v.make} {v.model}</td>
                        <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{v.service}</td>
                        <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{v.mechanic}</td>
                        <td className="px-4 py-3 font-mono text-xs text-gray-500">{v.startTime} → {v.estimatedEnd}</td>
                        <td className="px-4 py-3" style={{minWidth:120}}>
                          <div className="flex items-center gap-2">
                            <div className="flex-1 h-1.5 bg-gray-100 dark:bg-gray-800 rounded-full overflow-hidden">
                              <div className={['h-full rounded-full', c.bar].join(' ')} style={{width:`${v.progress}%`}} />
                            </div>
                            <span className="text-xs font-mono text-gray-500 shrink-0">{v.progress}%</span>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <span className={['inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium', c.badge, c.color].join(' ')}>{c.label}</span>
                        </td>
                        <td className="px-4 py-3">
                          <Link href={`/vehicles/${v.id}`} className="text-xs px-2.5 py-1.5 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition text-gray-600 dark:text-gray-400">Fiche →</Link>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </>)}

        {/* ── SERVICES ──────────────────────────────────────── */}
        {section === 'services' && (
          <div>
            <div className="flex items-center justify-between mb-5">
              <div>
                <h2 className="text-lg font-bold text-gray-900 dark:text-white">🗂 Tous les services</h2>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{SERVICES.length} services disponibles</p>
              </div>
            </div>
            <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-6">
              <ServicesMenu showFilter onServiceSelect={(id) => { setSelectedService(id); setSection('overview') }} selectedServiceId={selectedService ?? undefined} />
            </div>
            <div className="mt-6">
              <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">Services en vedette</h3>
              <div className="grid grid-cols-3 gap-4">
                {SERVICES.filter(s => ['garage','diagnostic','assistance'].includes(s.id)).map(s => (
                  <ServiceCard key={s.id} service={s} variant="featured" isSelected={s.id===selectedService} onSelect={(id) => { setSelectedService(id); setSection('overview') }} />
                ))}
              </div>
            </div>
          </div>
        )}

        {/* ── BAYS ──────────────────────────────────────────── */}
        {section === 'bays' && (
          <div>
            <div className="flex items-center justify-between mb-5">
              <div>
                <h2 className="text-lg font-bold text-gray-900 dark:text-white">👷 Postes de travail</h2>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
                  {BAYS.filter(b=>b.status==='working').length} occupé(s) · {BAYS.filter(b=>b.status==='available').length} disponible(s) · {BAYS.filter(b=>b.status==='break').length} en pause
                </p>
              </div>
              <Link href="/tasks" className="px-4 py-2 text-sm border border-gray-300 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition">Journal des tâches →</Link>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {BAYS.map(bay => {
                const c = BAY_CFG[bay.status]
                return (
                  <div key={bay.id} className={['bg-white dark:bg-gray-900 border rounded-2xl p-5', c.border].join(' ')}>
                    <div className="flex items-center gap-3 mb-4">
                      <div className="w-12 h-12 rounded-full bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center text-base font-bold text-amber-700 dark:text-amber-400">{bay.initials}</div>
                      <div>
                        <div className="font-semibold text-gray-900 dark:text-white">{bay.name}</div>
                        <div className="flex items-center gap-1.5 mt-0.5">
                          <span className={['w-2 h-2 rounded-full', c.dot].join(' ')} />
                          <span className="text-xs text-gray-500 dark:text-gray-400">{c.label}</span>
                        </div>
                      </div>
                    </div>
                    {bay.vehiclePlate ? (
                      <div>
                        <div className="flex items-center gap-2 mb-2">
                          <span className="font-mono text-sm bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400 px-2 py-0.5 rounded border border-amber-200 dark:border-amber-800">{bay.vehiclePlate}</span>
                          {bay.timeLeft && <span className="text-xs text-gray-400 ml-auto">⏱ {bay.timeLeft}</span>}
                        </div>
                        <div className="text-sm text-gray-700 dark:text-gray-300 mb-3">{bay.currentTask}</div>
                        {bay.progress !== undefined && (
                          <>
                            <div className="flex justify-between text-xs text-gray-400 mb-1.5"><span>Avancement</span><span>{bay.progress}%</span></div>
                            <div className="h-2 bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
                              <div className="h-full rounded-full" style={{width:`${bay.progress}%`, background: bay.progress>=80?'#22c55e':bay.progress>=40?'#f59e0b':'#3b82f6'}} />
                            </div>
                          </>
                        )}
                      </div>
                    ) : (
                      <div className="flex items-center justify-center h-14 text-sm text-gray-400">{bay.currentTask ?? 'Aucune tâche assignée'}</div>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        )}

        {/* ── VEHICLES ──────────────────────────────────────── */}
        {section === 'vehicles' && (
          <div>
            <div className="flex items-center justify-between mb-5">
              <div>
                <h2 className="text-lg font-bold text-gray-900 dark:text-white">🚗 Véhicules en atelier</h2>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{VEHICLES.length} véhicules en intervention</p>
              </div>
              <Link href="/vehicles/new" className="px-4 py-2 text-sm bg-amber-500 hover:bg-amber-600 text-white rounded-lg font-medium transition">+ Nouveau</Link>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {VEHICLES.map(v => {
                const c = VEH_CFG[v.status]
                return (
                  <div key={v.id} className={['bg-white dark:bg-gray-900 border rounded-2xl p-5', c.badge].join(' ')}>
                    <div className="flex items-start justify-between mb-3">
                      <div>
                        <div className="flex items-center gap-2 mb-1">
                          <span className="font-mono text-sm bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400 px-2 py-0.5 rounded border border-amber-200 dark:border-amber-800">{v.plate}</span>
                          <span className={['text-xs px-2 py-0.5 rounded-full font-medium', c.badge, c.color].join(' ')}>{c.label}</span>
                        </div>
                        <div className="font-semibold text-gray-900 dark:text-white">{v.make} {v.model}</div>
                        <div className="text-sm text-gray-600 dark:text-gray-400 mt-0.5">{v.service}</div>
                      </div>
                      <Link href={`/vehicles/${v.id}`} className="text-xs px-3 py-1.5 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition text-gray-600 dark:text-gray-400 shrink-0">Fiche →</Link>
                    </div>
                    <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400 mb-3">
                      <span>👷 {v.mechanic}</span>
                      <span>⏰ {v.startTime} → {v.estimatedEnd}</span>
                    </div>
                    <div>
                      <div className="flex justify-between text-xs text-gray-400 mb-1.5"><span>Avancement</span><span className="font-mono">{v.progress}%</span></div>
                      <div className="h-2 bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
                        <div className={['h-full rounded-full', c.bar].join(' ')} style={{width:`${v.progress}%`}} />
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )}

      </div>
    </div>
  )
}
