'use client'
import React, { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useDashboardStats, useFinanceMetrics } from '@/lib/hooks/useDashboard'
import { useCustomers, useCreateCustomer } from '@/lib/hooks/useCrm'
import { useVehicles, useCreateVehicle } from '@/lib/hooks/useVehicles'
import { useQueryClient } from '@tanstack/react-query'
import { useParts, useAdjustStock } from '@/lib/hooks/useInventory'
import { useInvoices } from '@/lib/hooks/useBilling'
import { useRevisions } from '@/lib/hooks/useOperations'
import { useChatHub } from '@/lib/hooks/useSignalR'

// ─────────────────────────────────────────────────────────────
// SHARED COMPONENTS
// ─────────────────────────────────────────────────────────────

function SvgGauge({ value, max, color, size = 128, label }: { value: number; max: number; color: string; size?: number; label: string }) {
  const r = (size / 2) * 0.8
  const circ = 2 * Math.PI * r
  const offset = circ - (circ * value) / max
  const cx = size / 2
  const cy = size / 2
  return (
    <div className="relative flex items-center justify-center" style={{ width: size, height: size }}>
      <svg className="absolute inset-0 w-full h-full -rotate-90" viewBox={`0 0 ${size} ${size}`}>
        <circle cx={cx} cy={cy} r={r} fill="transparent" stroke="#262626" strokeWidth="8" />
        <circle cx={cx} cy={cy} r={r} fill="transparent" stroke={color} strokeWidth="8"
          strokeDasharray={circ} strokeDashoffset={offset} strokeLinecap="round"
          style={{ filter: `drop-shadow(0 0 6px ${color})`, transition: 'stroke-dashoffset 0.8s ease' }} />
      </svg>
      <div className="absolute text-center z-10">
        <span className="font-headline font-black text-white block" style={{ fontSize: size * 0.22 }}>{value}</span>
        <span className="text-neutral-500 uppercase font-bold block" style={{ fontSize: size * 0.07 }}>{label}</span>
      </div>
    </div>
  )
}

function GlassCard({ children, className = "", accent = "", title }: { children: React.ReactNode; className?: string; accent?: string; title?: string }) {
  return (
    <div
      className={`glass-panel rounded-xl border border-white/5 p-6 relative overflow-hidden ${className}`}
      style={accent ? { borderLeftColor: accent, borderLeftWidth: 4 } : {}}
    >
      <div className="absolute top-0 left-0 w-full" style={{ height: 1, background: 'linear-gradient(90deg, transparent, rgba(255,107,0,0.3), transparent)' }} />
      {title && (
        <div className="flex justify-between items-center mb-6">
          <h3 className="font-headline text-[10px] font-bold uppercase tracking-[0.3em] text-neutral-500 flex items-center gap-2">
            <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B00] animate-pulse" />
            {title}
          </h3>
          <div className="h-[1px] flex-1 bg-white/5 ml-4" />
        </div>
      )}
      {children}
    </div>
  )
}

function NeumorphicCard({ children, className = "" }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`p-4 rounded-xl transition-all duration-300 hover:translate-x-1 ${className}`}
      style={{ background: '#1a1919', boxShadow: '4px 4px 8px #0a0a0a, -4px -4px 8px #2a2a2a' }}>
      {children}
    </div>
  )
}

function StatusBadge({ label, color = '#00eefc' }: { label: string; color?: string }) {
  return (
    <span className="text-[8px] font-headline font-bold uppercase tracking-widest px-3 py-1 rounded border" style={{ color, borderColor: color + '44', background: color + '11' }}>
      {label}
    </span>
  )
}

function PageHeader({ title, accent, subtitle }: { title: string; accent: string; subtitle?: string }) {
  return (
    <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-6 md:mb-10">
      <div>
        <h1 className="font-headline text-3xl md:text-5xl font-black text-white tracking-tight uppercase italic break-words">
          {title} <span className="text-[#FF6B00] not-italic">{accent}</span>
        </h1>
        {subtitle && <p className="text-neutral-600 text-[10px] md:text-xs uppercase tracking-widest font-mono mt-2">{subtitle}</p>}
      </div>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// 1. HUB — from dashboard_hub + telemetry_hud_3d
// ─────────────────────────────────────────────────────────────

export function HubPage() {
  const { data: stats, isLoading: statsLoading } = useDashboardStats()
  const { data: finance, isLoading: financeLoading } = useFinanceMetrics()

  return (
    <div className="space-y-8">
      {/* Laser Sweep bar */}
      <div className="fixed top-14 left-0 w-full h-0.5 z-40 overflow-hidden">
        <div className="w-full h-full animate-laser" />
      </div>

      <PageHeader title="WORKSHOP" accent="HUB" subtitle="Real-time telemetry · Automated task allocation" />

      {/* Big Gauge Row — Connected to real data */}
      <GlassCard className="flex flex-col md:flex-row justify-around items-center gap-10 py-8">
        <div className="relative">
          <SvgGauge 
            value={statsLoading ? 0 : stats?.vehiclesInProgress || 0} 
            max={20} 
            color="#FF6B00" 
            size={192} 
            label="Vehicles" 
          />
          <span className="absolute -top-4 left-1/2 -translate-x-1/2 text-[9px] font-headline uppercase tracking-widest text-neutral-600 bg-[#0e0e0e] px-2 whitespace-nowrap">Operational_Load</span>
        </div>
        <div className="relative">
          <SvgGauge 
            value={statsLoading ? 0 : stats?.activeDiagnostics || 0} 
            max={50} 
            color="#00eefc" 
            size={192} 
            label="Diag" 
          />
          <span className="absolute -top-4 left-1/2 -translate-x-1/2 text-[9px] font-headline uppercase tracking-widest text-neutral-600 bg-[#0e0e0e] px-2 whitespace-nowrap">Active_Analysis</span>
        </div>
        <div className="relative">
          <SvgGauge 
            value={statsLoading ? 0 : stats?.todayRevisions || 0} 
            max={15} 
            color="#ff7351" 
            size={192} 
            label="Plan" 
          />
          <span className="absolute -top-4 left-1/2 -translate-x-1/2 text-[9px] font-headline uppercase tracking-widest text-neutral-600 bg-[#0e0e0e] px-2 whitespace-nowrap">Daily_Queue</span>
        </div>
      </GlassCard>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
        {/* Finance Snapshot + System Log */}
        <div className="md:col-span-8 grid grid-cols-1 md:grid-cols-2 gap-6">
          <GlassCard accent="#00eefc">
            <p className="font-headline text-[10px] uppercase tracking-widest text-neutral-500 mb-4">Monthly_Revenue_Node</p>
            <div className="flex flex-col items-center justify-center h-32 relative">
              <p className="font-headline font-black text-4xl text-white tracking-widest">
                {financeLoading ? '---' : `${finance?.monthlyRevenue?.toLocaleString()} €`}
              </p>
              <div className="flex items-center gap-2 mt-2">
                <span className="text-[#34d399] text-[10px] font-mono">▲ {finance?.growthPercentage || 0}%</span>
                <span className="text-neutral-600 text-[10px] font-mono">vs prev month</span>
              </div>
            </div>
            <div className="flex justify-between border-t border-white/5 pt-4 mt-2">
              <div className="text-center flex-1 border-r border-white/5">
                <p className="text-[8px] text-neutral-600">PENDING</p>
                <p className="font-mono text-xs text-[#FF6B00]">{financeLoading ? '--' : `${finance?.pendingRevenue?.toLocaleString()} €`}</p>
              </div>
              <div className="text-center flex-1">
                <p className="text-[8px] text-neutral-600">INVOICES</p>
                <p className="font-mono text-xs text-white">{finance?.totalInvoices || 0}</p>
              </div>
            </div>
          </GlassCard>

          <GlassCard>
            <div className="flex justify-between mb-4">
              <p className="font-headline text-[10px] uppercase tracking-widest text-neutral-500">Live_System_Trace</p>
              <StatusBadge label={statsLoading ? "IDLE" : "Link_Established"} color={statsLoading ? "#666" : "#FF6B00"} />
            </div>
            <div className="space-y-2 font-mono text-[10px]">
              <div className="flex justify-between text-neutral-400">
                <span className="text-neutral-700 font-bold">14:12:08</span>
                <span>SEC_SCAN — Access verified</span>
              </div>
              <div className="flex justify-between text-neutral-400">
                <span className="text-neutral-700 font-bold">14:24:22</span>
                <span>DB_NODE — Connection stable</span>
              </div>
              <div className="flex justify-between text-[#00eefc]">
                <span className="text-neutral-700 font-bold">14:31:01</span>
                <span>DATA_SYNC — {stats?.totalClients || 0} Clients Loaded</span>
              </div>
              <div className="flex justify-between text-[#FF6B00]">
                <span className="text-neutral-700 font-bold">15:05:44</span>
                <span>OP_LEVEL — {stats?.todayRevisions || 0} Daily queue items</span>
              </div>
            </div>
          </GlassCard>
        </div>

        {/* Quick Tiles + AI Confidence */}
        <div className="md:col-span-4 space-y-4">
          <div className="grid grid-cols-2 gap-3">
            {[
              { icon: 'battery_charging_full', label: 'Core_Charge', value: '84%', color: '#00eefc' },
              { icon: 'oil_barrel', label: 'Fluid_Pressure', value: '42 PSI', color: '#FF6B00' },
              { icon: 'tire_repair', label: 'Tire_Wear', value: 'OPTIMAL', color: '#00eefc' },
              { icon: 'settings_input_antenna', label: 'Data_Uplink', value: 'STABLE', color: '#00eefc' },
            ].map(t => (
              <GlassCard key={t.label} className="p-4 cursor-pointer hover:border-[#FF6B00]/20 transition-all">
                <span className="material-symbols-outlined mb-2 block" style={{ color: t.color }}>{t.icon}</span>
                <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{t.label}</p>
                <p className="font-headline font-bold text-base text-white">{t.value}</p>
              </GlassCard>
            ))}
          </div>
          <GlassCard className="text-center">
            <p className="font-headline text-[10px] uppercase tracking-widest text-neutral-500 mb-3">AI_Drive_Confidence</p>
            <div className="flex justify-center"><SvgGauge value={90} max={100} color="#00eefc" size={120} label="%" /></div>
          </GlassCard>
        </div>
      </div>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// 2. PLANNING — Workshop schedule
// ─────────────────────────────────────────────────────────────

export function PlanningPage() {
  const { data: revisionsData, isLoading } = useRevisions(1, 100)
  const safeItems = revisionsData?.items || []

  // Simple load calculation based on status for visual flair
  const getStatusDetails = (status: string) => {
    switch(status) {
      case 'InProgress': return { color: '#00eefc', load: 85, label: 'EN COURS' }
      case 'Completed': return { color: '#34d399', load: 100, label: 'TERMINÉ' }
      case 'Scheduled': return { color: '#FF6B00', load: 15, label: 'PLANIFIÉ' }
      case 'Cancelled': return { color: '#ff7351', load: 0, label: 'ANNULÉ' }
      default: return { color: '#adaaaa', load: 0, label: status }
    }
  }

  return (
    <div className="space-y-8">
      <PageHeader title="ATELIER" accent="PLANNING" subtitle="Allocation baies · Charge tech · Interventions" />
      <GlassCard title="ORDRES DE RÉPARATION ACTIFS">
        <div className="overflow-x-auto -mx-6 px-6">
          <table className="w-full text-left text-sm min-w-[600px]">
            <thead>
              <tr className="text-[9px] font-headline uppercase tracking-widest text-neutral-600 border-b border-white/5 pb-2">
                <th className="py-3 px-4">Intervention #</th>
                <th className="py-3 px-4">Véhicule</th>
                <th className="py-3 px-4">Mécanicien</th>
                <th className="py-3 px-4">Charge / Avancement</th>
                <th className="py-3 px-4 text-right">Statut</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5 disabled:opacity-50">
              {isLoading ? (
                <tr><td colSpan={5} className="py-8 text-center text-neutral-500 animate-pulse">Scan en cours...</td></tr>
              ) : safeItems.length === 0 ? (
                <tr><td colSpan={5} className="py-8 text-center text-neutral-500">Aucune intervention</td></tr>
              ) : safeItems.map(o => {
                const details = getStatusDetails(o.status)
                return (
                  <tr key={o.id} className="hover:bg-white/5 transition-all">
                    <td className="py-4 px-4 font-mono text-[10px] text-neutral-400">
                      <p className="text-[#FF6B00] mb-1">{o.id.substring(0,8)}</p>
                      {new Date(o.scheduledDate).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                    </td>
                    <td className="py-4 px-4 font-headline font-bold text-white uppercase text-xs tracking-wider">
                      {o.vehicleName || 'N/A'}
                      <p className="text-[9px] text-neutral-500 lowercase normal-case mt-1">{o.description || 'Routine check'}</p>
                    </td>
                    <td className="py-4 px-4 text-neutral-400 text-xs flex items-center gap-2">
                       <span className="material-symbols-outlined text-sm text-[#00eefc]">engineering</span>
                       {o.mechanicName || 'Auto-assign'}
                    </td>
                    <td className="py-4 px-4 w-40">
                      <div className="h-1 bg-white/5 rounded-full overflow-hidden">
                        <div className="h-full rounded-full transition-all duration-1000" style={{ width: `${details.load}%`, backgroundColor: details.color, boxShadow: `0 0 8px ${details.color}` }} />
                      </div>
                    </td>
                    <td className="py-4 px-4 text-right"><StatusBadge label={details.label} color={details.color} /></td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </GlassCard>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// 3. DIAGNOSTICS — AI Vision + Engine Blueprint from engine_blueprint_3d
// ─────────────────────────────────────────────────────────────

export function DiagnosticsPage() {
  const parts = [
    { icon: 'settings', name: 'Cylinder Head', health: 92, status: 'OPTIMAL', color: '#00eefc' },
    { icon: 'bolt', name: 'Ignition Coil', health: 45, status: 'MAINTENANCE', color: '#FF6B00' },
    { icon: 'ac_unit', name: 'Cooling Pump', health: 88, status: 'OPTIMAL', color: '#00eefc' },
  ]
  return (
    <div className="space-y-8">
      <PageHeader title="ENGINE" accent="DIAGNOSTICS" subtitle="V12-HYBRID_PROTOTYPE · Serial: #ND-992-X" />
      <div className="grid grid-cols-1 md:grid-cols-12 gap-6 min-h-[500px]">
        {/* Left gauges */}
        <div className="md:col-span-3 flex flex-col gap-6">
          <GlassCard className="border-l-4 border-l-[#00eefc]">
            <div className="flex justify-between mb-4">
              <p className="text-[10px] font-headline uppercase tracking-widest text-neutral-500">Core Temp</p>
              <span className="material-symbols-outlined text-[#00eefc] text-sm">thermostat</span>
            </div>
            <div className="flex justify-center"><SvgGauge value={84} max={120} color="#00eefc" size={128} label="°C Optimal" /></div>
          </GlassCard>
          <GlassCard className="border-l-4 border-l-[#FF6B00]">
            <div className="flex justify-between mb-4">
              <p className="text-[10px] font-headline uppercase tracking-widest text-neutral-500">Oil Pressure</p>
              <span className="material-symbols-outlined text-[#FF6B00] text-sm">opacity</span>
            </div>
            <div className="flex justify-center"><SvgGauge value={62} max={100} color="#FF6B00" size={128} label="PSI" /></div>
          </GlassCard>
        </div>

        {/* Center: blueprint overlay */}
        <div className="md:col-span-6 relative flex items-center justify-center bg-black/30 rounded-xl border border-white/5 overflow-hidden min-h-[400px]">
          <div className="absolute inset-0 flex items-center justify-center opacity-30 mix-blend-screen">
            <span className="material-symbols-outlined text-[#00eefc]" style={{ fontSize: '280px', fontVariationSettings: "'FILL' 0, 'wght' 100" }}>settings</span>
          </div>
          {/* Neon annotation lines */}
          <div className="absolute top-1/4 left-1/4 glass-panel px-3 py-1 rounded text-[9px] font-mono text-[#00eefc] border border-[#00eefc]/30 hidden md:block" style={{ boxShadow: '0 0 8px rgba(0,238,252,0.3)' }}>
            PISTON_ASSEMBLY [L-SIDE]
          </div>
          <div className="absolute bottom-1/3 right-1/4 glass-panel px-3 py-1 rounded text-[9px] font-mono text-[#FF6B00] border border-[#FF6B00]/30 hidden md:block" style={{ boxShadow: '0 0 8px rgba(255,107,0,0.3)' }}>
            TURBOCHARGER_UNIT [A1]
          </div>
          <div className="absolute top-0 left-0 w-full h-0.5 animate-laser" />
        </div>

        {/* Right: component health */}
        <div className="md:col-span-3 flex flex-col gap-4">
          <p className="font-headline text-xs font-bold uppercase tracking-[0.2em] text-neutral-500">Component Health</p>
          {parts.map(p => (
            <NeumorphicCard key={p.name}>
              <div className="flex items-center gap-4">
                <div className="w-10 h-10 rounded-lg flex items-center justify-center border" style={{ borderColor: p.color + '33' }}>
                  <span className="material-symbols-outlined text-sm" style={{ color: p.color }}>{p.icon}</span>
                </div>
                <div className="flex-1">
                  <p className="text-xs font-bold uppercase text-white">{p.name}</p>
                  <div className="flex items-center gap-2 mt-1">
                    <div className="h-1 flex-1 bg-black/40 rounded-full">
                      <div className="h-full rounded-full" style={{ width: `${p.health}%`, backgroundColor: p.color, boxShadow: `0 0 6px ${p.color}` }} />
                    </div>
                    <span className="text-[8px] font-mono" style={{ color: p.color }}>{p.status}</span>
                  </div>
                </div>
              </div>
            </NeumorphicCard>
          ))}
          <button className="mt-2 py-4 w-full bg-gradient-to-r from-[#FF6B00] to-[#FF7A2F] rounded-xl font-headline font-black uppercase tracking-widest text-black text-sm transition-all hover:brightness-110 active:scale-95" style={{ boxShadow: '0 0 20px rgba(255,107,0,0.3)' }}>
            Initiate Stress Test
          </button>
        </div>
      </div>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// 4. INVENTAIRE
// ─────────────────────────────────────────────────────────────

export function InventairePage() {
  return (
    <div className="space-y-8">
      <PageHeader title="LOGISTICS" accent="INVENTORY" subtitle="Real-time stock telemetry · Auto-replenishment" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { icon: 'inventory_2', value: '1,420', label: 'References', color: '#00eefc' },
          { icon: 'warning', value: '05', label: 'Critical Low', color: '#ff7351' },
          { icon: 'warehouse', value: '88%', label: 'Zone A Capacity', color: '#FF6B00' },
          { icon: 'local_shipping', value: '3', label: 'Orders Pending', color: '#00eefc' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard>
        <div className="font-mono text-[10px] space-y-3 text-neutral-400">
          <div className="flex gap-2 items-center"><span className="text-[#00eefc]">[IN]</span> BOSCH_BRAKE_PADS – 24 UNITS – BIN_A12</div>
          <div className="flex gap-2 items-center"><span className="text-[#FF6B00]">[OUT]</span> CASTROL_OIL_5W30 – 5 UNITS – BIN_C04</div>
          <div className="flex gap-2 items-center"><span className="text-[#ff7351]">[ALERT]</span> FILTER_AIR_PURFLUX – STOCK&lt;2 – REORDER_REQUIRED</div>
        </div>
      </GlassCard>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// 5. FACTURATION
// ─────────────────────────────────────────────────────────────

export function FacturationPage() {
  const { data: invoices, isLoading } = useInvoices()
  const { data: finance } = useFinanceMetrics()

  const safeInvoices = invoices || []

  return (
    <div className="space-y-8">
      <PageHeader title="FINANCIAL" accent="LEDGER" subtitle="Facturation · Trésorerie · Encaissements" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Chiffre d\'Affaires (Mois)', value: `${finance?.monthlyRevenue?.toLocaleString() || 0} €`, color: '#00eefc' },
          { label: 'Factures en Attente', value: `${finance?.pendingRevenue?.toLocaleString() || 0} €`, color: '#ff7351' },
          { label: 'Factures Émises', value: `${finance?.totalInvoices || 0}`, color: '#FF6B00' },
          { label: 'Croissance', value: `+${finance?.growthPercentage || 0}%`, color: '#34d399' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="REGISTRE DES FACTURES">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-[9px] font-headline uppercase tracking-widest text-neutral-600 border-b border-white/5">
                <th className="py-3 px-4 text-left">N° Facture</th>
                <th className="py-3 px-4 text-left">Client</th>
                <th className="py-3 px-4 text-left">Montant TTC</th>
                <th className="py-3 px-4 text-left">Date Émission</th>
                <th className="py-3 px-4 text-right">Statut</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5 text-xs text-white">
              {isLoading ? (
                <tr><td colSpan={5} className="py-8 text-center animate-pulse text-neutral-500">Chargement du grand livre...</td></tr>
              ) : safeInvoices.length === 0 ? (
                <tr><td colSpan={5} className="py-8 text-center text-neutral-500">Aucune facture enregistrée</td></tr>
              ) : safeInvoices.map(i => (
                <tr key={i.id} className="hover:bg-white/5 transition-all">
                  <td className="py-4 px-4 font-mono text-[10px] text-[#00eefc]">{i.invoiceNumber || i.id.substring(0,8)}</td>
                  <td className="py-4 px-4 font-bold">{i.customerName || 'Client inconnu'}</td>
                  <td className="py-4 px-4 font-mono text-[#FF6B00]">{(i.totalTTC || 0).toLocaleString()} €</td>
                  <td className="py-4 px-4 text-neutral-400">{new Date(i.issuedAt).toLocaleDateString()}</td>
                  <td className="py-4 px-4 text-right">
                    <StatusBadge 
                      label={i.status} 
                      color={i.status === 'Paid' ? '#34d399' : i.status === 'Draft' || i.status === 'Sent' ? '#FF6B00' : '#ff7351'} 
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </GlassCard>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// 6. SUPPORT CHAT — AI Technical Assistant
// ─────────────────────────────────────────────────────────────

export function SupportChatPage() {
  const [msg, setMsg] = useState('')
  const { connection, messages: signalRMessages, sendMessage } = useChatHub('00000000-0000-0000-0000-000000000000')

  const handleSend = async (e?: React.FormEvent) => {
    e?.preventDefault()
    if (!msg.trim()) return
    await sendMessage(msg)
    setMsg('')
  }

  // Combine mock messages and real-time ones
  const baseMessages = [
    { senderId: 'CLAUDE_AI', content: 'Analyzed DRIVE_TRAIN_M4. Hydraulic pressure nominal. Suggesting 15% torque reduction on primary axis to compensate thermal oscillation.', sentAt: new Date(Date.now() - 1000 * 60 * 5).toISOString() },
    { senderId: 'OPERATOR', content: 'Acknowledged. Will this impact our sub-40s cycle time?', sentAt: new Date(Date.now() - 1000 * 60 * 3).toISOString() },
    { senderId: 'CLAUDE_AI', content: 'Projections show < 1.2s impact. Z-AXIS velocity optimized to compensate. Current cycle: 38.2s → Projected: 39.4s.', sentAt: new Date(Date.now() - 1000 * 60 * 2).toISOString() },
  ]
  const allMessages = [...baseMessages, ...signalRMessages]

  return (
    <div className="flex flex-col rounded-xl border border-white/5 overflow-hidden animate-fade h-[500px] md:h-[700px]">
      <header className="px-4 md:px-8 py-5 border-b border-white/5 flex flex-col md:flex-row justify-between items-start md:items-center bg-black/60 gap-4">
        <div>
          <h2 className="font-headline font-bold text-white uppercase tracking-[0.2em] text-lg">AI ASSISTANT <span className="text-[#00eefc]">LINK</span></h2>
          <div className="flex items-center gap-2 mt-1">
            <span className={`w-1.5 h-1.5 rounded-full ${connection ? 'bg-[#00eefc] animate-pulse' : 'bg-[#ff7351]'}`} style={{ boxShadow: `0 0 8px ${connection ? '#00eefc' : '#ff7351'}` }} />
            <p className="text-[9px] font-mono text-neutral-600 uppercase tracking-[0.2em]">SECURE_LINK // Claude 3.5 Sonnet // AES-256</p>
          </div>
        </div>
        <div className="flex gap-2">
          {['history', 'settings'].map(ic => (
            <button key={ic} className="w-9 h-9 flex items-center justify-center rounded border border-white/10 hover:border-[#00eefc]/40 transition-colors">
              <span className="material-symbols-outlined text-neutral-500 text-sm">{ic}</span>
            </button>
          ))}
        </div>
      </header>

      <div className="flex-1 overflow-y-auto p-4 md:p-8 space-y-6 bg-[#0a0a0a]">
        <div className="flex justify-center">
          <span className="text-[8px] font-mono text-neutral-800 uppercase tracking-[0.2em] bg-black px-4 py-1 rounded-full border border-white/5">
            SESSION_START: 2026-03-24 T14:22:01Z
          </span>
        </div>
        {allMessages.map((m: any, i) => {
          const isAi = m.senderId === 'CLAUDE_AI'
          return (
            <div key={i} className={`flex gap-3 max-w-[85%] md:max-w-[70%] ${isAi ? 'mr-auto' : 'ml-auto flex-row-reverse'}`}>
              <div className={`w-9 h-9 rounded-lg flex items-center justify-center shrink-0 border ${isAi ? 'border-[#00eefc]/20 bg-black' : 'border-[#FF6B00]/20 bg-[#FF6B00]/5'}`}>
                <span className={`material-symbols-outlined text-base ${isAi ? 'text-[#00eefc]' : 'text-[#FF6B00]'}`} style={{ fontVariationSettings: "'FILL' 1" }}>
                  {isAi ? 'smart_toy' : 'person'}
                </span>
              </div>
              <div className={`flex flex-col gap-1 ${isAi ? '' : 'items-end'}`}>
                <div className={`p-4 rounded-xl text-xs leading-relaxed backdrop-blur-sm border-l-2 ${isAi ? 'border-[#00eefc] bg-[#00eefc]/5 text-white/90' : 'border-[#FF6B00] bg-[#FF6B00]/5 text-white/90'}`}>
                  {m.content}
                </div>
                <span className="font-mono text-[8px] text-neutral-700 uppercase">{m.senderId} · {new Date(m.sentAt).toLocaleTimeString()}</span>
              </div>
            </div>
          )
        })}
      </div>

      <div className="p-4 md:p-6 bg-black/80 border-t border-white/5">
        <div className="flex gap-2 flex-wrap mb-3">
          {['RUN DIAGNOSTIC', 'FETCH LOGS', 'SYSTEM STATUS'].map(q => (
            <button key={q} onClick={() => setMsg(q)} className="px-3 py-1 text-[9px] font-mono uppercase tracking-widest rounded-full border border-white/10 text-neutral-500 hover:text-[#00eefc] hover:border-[#00eefc]/30 transition-colors">
              {q}
            </button>
          ))}
        </div>
        <form onSubmit={handleSend} className="relative flex items-center bg-[#1a1919] border border-white/10 rounded-lg focus-within:border-[#00eefc]/50 transition-all">
          <button type="button" className="px-3 text-neutral-700 hover:text-[#00eefc]"><span className="material-symbols-outlined text-sm">attach_file</span></button>
          <input value={msg} onChange={e => setMsg(e.target.value)}
            className="flex-1 bg-transparent py-4 text-xs text-white placeholder:text-neutral-800 outline-none font-mono"
            placeholder="INPUT_QUERY: Type your technical inquiry..." />
          <button type="submit" className="mr-3 bg-[#FF6B00] text-black px-4 py-1.5 rounded font-headline font-bold text-[9px] uppercase tracking-[0.15em] flex items-center gap-1 hover:brightness-110" style={{ boxShadow: '0 0 12px rgba(255,107,0,0.3)' }}>
            Send <span className="material-symbols-outlined text-sm">send</span>
          </button>
        </form>
        <p className="text-[8px] font-mono text-neutral-700 mt-2 uppercase tracking-[0.2em] flex items-center gap-2">
          <span>Model: <span className="text-[#00eefc]">Claude 3.5 Sonnet</span></span>
          <span className="text-[#34d399] tracking-widest">{connection ? '🟢 CONNECTED' : '🔴 OFFLINE'}</span>
        </p>
      </div>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// 7. EXPLORATEUR — System Module Grid
// ─────────────────────────────────────────────────────────────

export function ExplorateurPage() {
  const router = useRouter()
  const modules = [
    { label: 'WORKSHOP HUB', icon: 'grid_view', href: '/hub', sub: 'Real-time telemetry', color: '#FF6B00' },
    { label: 'RESOURCE PLANNING', icon: 'precision_manufacturing', href: '/planning', sub: 'Bay allocation', color: '#00eefc' },
    { label: 'INVENTORY NODE', icon: 'inventory_2', href: '/inventaire', sub: 'Stock telemetry', color: '#00eefc' },
    { label: 'ENGINE DIAGNOSTICS', icon: 'visibility', href: '/diagnostics', sub: 'Neural vision AI', color: '#FF6B00' },
    { label: 'FINANCIAL LEDGER', icon: 'receipt_long', href: '/facturation', sub: 'Billing & treasury', color: '#00eefc' },
    { label: 'AI ASSISTANT', icon: 'smart_toy', href: '/support-chat', sub: 'Claude 3.5 Sonnet', color: '#FF6B00' },
    { label: 'CLIENT DATABASE', icon: 'hub', href: '/crm', sub: 'CRM node', color: '#00eefc' },
    { label: 'VEHICLE FLEET', icon: 'directions_car', href: '/parc-vehicules', sub: 'Fleet telemetry', color: '#FF6B00' },
    { label: 'PARTS CATALOGUE', icon: 'library_books', href: '/catalogue', sub: 'Parts database', color: '#00eefc' },
  ]

  return (
    <div className="space-y-8">
      <PageHeader title="SYSTEM" accent="MODULES" subtitle="Select a core unit to initiate a secure connection" />
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {modules.map(m => (
          <button key={m.href} onClick={() => router.push(m.href)}
            className="group flex flex-col p-6 rounded-xl border border-white/5 bg-[#1a1919] text-left transition-all hover:border-[#FF6B00]/30 hover:bg-[#FF6B00]/5 relative overflow-hidden"
            style={{ boxShadow: 'inset 0 0 0 0 rgba(255,107,0,0)' }}>
            <div className="absolute top-0 left-0 w-full h-[1px] bg-gradient-to-r from-transparent via-white/5 to-transparent group-hover:via-[#FF6B00]/30 transition-all" />
            <span className="material-symbols-outlined text-2xl text-neutral-700 mb-4 group-hover:text-[#FF6B00] transition-colors">{m.icon}</span>
            <span className="font-headline font-bold text-white text-xs uppercase tracking-[0.2em] mb-1 group-hover:text-[#FF6B00] transition-colors">{m.label}</span>
            <span className="text-[9px] font-mono text-neutral-600 uppercase tracking-widest">{m.sub}</span>
            <span className="absolute right-4 bottom-4 material-symbols-outlined text-neutral-800 text-sm group-hover:text-[#FF6B00] transition-colors">arrow_forward</span>
          </button>
        ))}
      </div>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// STUB MODULES — Real implementations
// ─────────────────────────────────────────────────────────────

export function TachesPage() {
  const tasks = [
    { id: 'T-001', title: 'Vidange moteur BMW X5', vehicle: 'BMW X5 - AB-123-CD', priority: 'HIGH', tech: 'Marc D.', due: '25/03', status: 'En cours', color: '#FF6B00' },
    { id: 'T-002', title: 'Remplacement plaquettes', vehicle: 'Renault Clio - XY-456-ZA', priority: 'MED', tech: 'Jean L.', due: '26/03', status: 'Planifié', color: '#00eefc' },
    { id: 'T-003', title: 'Diagnostic électrique', vehicle: 'Peugeot 308 - KL-789-MN', priority: 'LOW', tech: 'Sophie R.', due: '27/03', status: 'En attente', color: '#94a3b8' },
    { id: 'T-004', title: 'Remplacement courroie', vehicle: 'Citroën C3 - OP-012-QR', priority: 'HIGH', tech: 'Marc D.', due: '28/03', status: 'Planifié', color: '#FF6B00' },
  ]
  return (
    <div className="space-y-8">
      <PageHeader title="JOURNAL" accent="DES TÂCHES" subtitle="Suivi temps réel · Ordres de travail · Affectation techniciens" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'En cours', value: '8', color: '#FF6B00', icon: 'cached' },
          { label: 'Planifiées', value: '14', color: '#00eefc', icon: 'event' },
          { label: 'Terminées', value: '32', color: '#34d399', icon: 'check_circle' },
          { label: 'En retard', value: '2', color: '#ff7351', icon: 'warning' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="ORDRES DE TRAVAIL ACTIFS">
        <div className="overflow-x-auto -mx-6 px-6">
          <table className="w-full text-sm min-w-[700px]">
            <thead>
              <tr className="text-[9px] font-headline uppercase tracking-widest text-neutral-600 border-b border-white/5">
                <th className="py-3 px-4 text-left">ID</th>
                <th className="py-3 px-4 text-left">Tâche</th>
                <th className="py-3 px-4 text-left">Véhicule</th>
                <th className="py-3 px-4 text-left">Technicien</th>
                <th className="py-3 px-4 text-left">Priorité</th>
                <th className="py-3 px-4 text-left">Échéance</th>
                <th className="py-3 px-4 text-right">Statut</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5">
              {tasks.map(t => (
                <tr key={t.id} className="hover:bg-white/5 transition-all">
                  <td className="py-4 px-4 font-mono text-[10px] text-neutral-600">{t.id}</td>
                  <td className="py-4 px-4 font-bold text-white text-xs">{t.title}</td>
                  <td className="py-4 px-4 text-neutral-400 text-xs">{t.vehicle}</td>
                  <td className="py-4 px-4 text-neutral-400 text-xs">{t.tech}</td>
                  <td className="py-4 px-4"><StatusBadge label={t.priority} color={t.color} /></td>
                  <td className="py-4 px-4 font-mono text-[10px] text-neutral-500">{t.due}</td>
                  <td className="py-4 px-4 text-right"><StatusBadge label={t.status} color={t.color} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </GlassCard>
    </div>
  )
}

export function CrmPage() {
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', phoneNumber: '' })
  
  const { data: clients, isLoading } = useCustomers(1, 20)
  const createMutation = useCreateCustomer()
  
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    createMutation.mutate(form, {
      onSuccess: () => {
        setShowAdd(false)
        setForm({ firstName: '', lastName: '', email: '', phoneNumber: '' })
      }
    })
  }

  return (
    <div className="space-y-8">
      <div className="flex justify-between items-end">
        <PageHeader title="GESTION" accent="CLIENTS CRM" subtitle="Base clients · Historique · Fidélisation" />
        <button 
          onClick={() => setShowAdd(!showAdd)}
          className={`mb-2 px-6 py-2 rounded-lg font-headline font-black text-[10px] tracking-widest uppercase transition-all flex items-center gap-2 ${
            showAdd ? 'bg-white/10 text-white' : 'bg-[#FF6B00] text-black shadow-[0_0_15px_rgba(255,107,0,0.4)]'
          }`}
        >
          <span className="material-symbols-outlined text-sm">{showAdd ? 'close' : 'person_add'}</span>
          {showAdd ? 'Annuler' : 'Nouveau Client'}
        </button>
      </div>

      {showAdd && (
        <GlassCard accent="#FF6B00" className="animate-in fade-in slide-in-from-top-4 duration-300">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Prénom</label>
                <input 
                  required
                  className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#FF6B00]/50 outline-none"
                  value={form.firstName}
                  onChange={e => setForm({...form, firstName: e.target.value})}
                />
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Nom</label>
                <input 
                  required
                  className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#FF6B00]/50 outline-none"
                  value={form.lastName}
                  onChange={e => setForm({...form, lastName: e.target.value})}
                />
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Email</label>
                <input 
                  type="email"
                  required
                  className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#FF6B00]/50 outline-none"
                  value={form.email}
                  onChange={e => setForm({...form, email: e.target.value})}
                />
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Téléphone</label>
                <input 
                  className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#FF6B00]/50 outline-none"
                  value={form.phoneNumber}
                  onChange={e => setForm({...form, phoneNumber: e.target.value})}
                />
              </div>
            </div>
            <div className="flex justify-end">
              <button 
                type="submit"
                disabled={createMutation.isPending}
                className="bg-[#FF6B00] text-black px-8 py-2 rounded font-headline font-black text-[10px] tracking-widest uppercase disabled:opacity-50"
              >
                {createMutation.isPending ? 'Enregistrement...' : 'Confirmer Registration'}
              </button>
            </div>
            {createMutation.isError && <p className="text-red-500 text-[10px] font-mono">ERROR: {createMutation.error.message}</p>}
          </form>
        </GlassCard>
      )}

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Clients actifs', value: clients?.totalCount || '---', color: '#00eefc', icon: 'group' },
          { label: 'Nouveaux ce mois', value: '12', color: '#34d399', icon: 'person_add' },
          { label: 'Contrats PRO', value: '18', color: '#FF6B00', icon: 'handshake' },
          { label: 'Taux fidélité', value: '91%', color: '#a78bfa', icon: 'favorite' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="ANNUAIRE CLIENTS">
        {isLoading ? (
          <div className="animate-pulse space-y-4">
            {[1,2,3].map(i => <div key={i} className="h-16 bg-white/5 rounded-lg" />)}
          </div>
        ) : (
          <div className="space-y-3">
            {clients?.items.map(c => (
              <div key={c.id} className="flex items-center gap-4 p-4 rounded-lg bg-white/3 hover:bg-white/5 transition-all border border-white/5">
                <div className="w-10 h-10 rounded-full flex items-center justify-center font-headline font-black text-sm" style={{ background: '#FF6B0022', color: '#FF6B00' }}>
                  {c.firstName?.[0] || '?'}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-bold text-white text-xs">{c.firstName} {c.lastName}</p>
                  <p className="text-neutral-500 text-[10px] truncate">{c.email}</p>
                </div>
                <div className="hidden md:flex flex-col items-end">
                  <p className="font-mono text-[#FF6B00] text-xs">{c.loyaltyPoints} PTS</p>
                  <p className="text-neutral-600 text-[10px]">Visite: {c.lastVisit || 'N/A'}</p>
                </div>
                <StatusBadge label={c.loyaltyPoints > 100 ? "VIP" : "Reg"} color={c.loyaltyPoints > 100 ? "#FF6B00" : "#94a3b8"} />
              </div>
            ))}
          </div>
        )}
      </GlassCard>
    </div>
  )
}

export function ParcVehiculesPage() {
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ brand: '', model: '', plate: '', vin: '', year: new Date().getFullYear(), mileage: 0, customerId: '' })
  
  const { data: fleet, isLoading } = useVehicles(1, 100)
  const { data: clients } = useCustomers(1, 100) // For dropdown
  const createMutation = useCreateVehicle()
  
  const items = fleet?.items || fleet || []

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    createMutation.mutate({
      brand: form.brand,
      model: form.model,
      licensePlate: form.plate,
      vin: form.vin,
      year: form.year,
      mileage: form.mileage,
      customerId: form.customerId
    }, {
      onSuccess: () => {
        setShowAdd(false)
        setForm({ brand: '', model: '', plate: '', vin: '', year: new Date().getFullYear(), mileage: 0, customerId: '' })
      }
    })
  }

  return (
    <div className="space-y-8">
      <div className="flex justify-between items-end">
        <PageHeader title="PARC" accent="VÉHICULES" subtitle="Flotte active · Historique maintenance" />
        <button 
          onClick={() => setShowAdd(!showAdd)}
          className={`mb-2 px-6 py-2 rounded-lg font-headline font-black text-[10px] tracking-widest uppercase transition-all flex items-center gap-2 ${
            showAdd ? 'bg-white/10 text-white' : 'bg-[#00eefc] text-black shadow-[0_0_15px_rgba(0,238,252,0.4)]'
          }`}
        >
          <span className="material-symbols-outlined text-sm">{showAdd ? 'close' : 'add_circle'}</span>
          {showAdd ? 'Annuler' : 'Nouveau Véhicule'}
        </button>
      </div>

      {showAdd && (
        <GlassCard accent="#00eefc" className="animate-in fade-in slide-in-from-top-4 duration-300">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="md:col-span-2">
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Propriétaire (Client)</label>
                <select 
                  required
                  className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none appearance-none"
                  value={form.customerId}
                  onChange={e => setForm({...form, customerId: e.target.value})}
                >
                  <option value="" className="bg-neutral-900">-- Sélectionner un client --</option>
                  {clients?.items.map(c => (
                    <option key={c.id} value={c.id} className="bg-neutral-900">{c.firstName} {c.lastName} ({c.email})</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Plaque d'immatriculation</label>
                <input 
                  required
                  className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none uppercase font-mono"
                  placeholder="AA-123-BB"
                  value={form.plate}
                  onChange={e => setForm({...form, plate: e.target.value})}
                />
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Marque</label>
                <input required className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none" value={form.brand} onChange={e => setForm({...form, brand: e.target.value})} />
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Modèle</label>
                <input required className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none" value={form.model} onChange={e => setForm({...form, model: e.target.value})} />
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Année</label>
                <input type="number" required className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none font-mono" value={form.year} onChange={e => setForm({...form, year: parseInt(e.target.value)})} />
              </div>
              <div className="md:col-span-2">
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">VIN (Code Châssis)</label>
                <input required className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none font-mono tracking-widest" value={form.vin} onChange={e => setForm({...form, vin: e.target.value})} />
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Kilométrage</label>
                <input type="number" required className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none font-mono" value={form.mileage} onChange={e => setForm({...form, mileage: parseInt(e.target.value)})} />
              </div>
            </div>
            <div className="flex justify-end">
              <button 
                type="submit"
                disabled={createMutation.isPending}
                className="bg-[#00eefc] text-black px-8 py-2 rounded font-headline font-black text-[10px] tracking-widest uppercase disabled:opacity-50 shadow-[0_0_10px_rgba(0,238,252,0.3)] hover:shadow-[0_0_20px_rgba(0,238,252,0.5)] transition-all"
              >
                {createMutation.isPending ? 'Sync en cours...' : 'Enregistrer Entité'}
              </button>
            </div>
            {createMutation.isError && <p className="text-red-400 text-[10px] font-mono">CODE_ERR: {createMutation.error.message}</p>}
          </form>
        </GlassCard>
      )}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 animate-pulse">
           {[1,2,3,4].map(i => <div key={i} className="h-32 bg-white/5 rounded-xl border border-white/5" />)}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {items.map((v: any) => (
            <GlassCard key={v.id} className="hover:border-white/10 transition-all">
              <div className="flex items-start gap-4">
                <span className="material-symbols-outlined text-3xl mt-1 text-[#00eefc]">directions_car</span>
                <div className="flex-1">
                  <div className="flex justify-between items-start gap-2 flex-wrap">
                    <div>
                      <p className="font-headline font-black text-white text-sm">{v.brand} {v.model}</p>
                      <p className="font-mono text-[10px] text-neutral-600 mt-0.5">{v.licensePlate} · {v.customerName || 'N/A'}</p>
                    </div>
                    <span className="font-mono text-xs text-[#00eefc]">{v.mileage?.toLocaleString()} km</span>
                  </div>
                  <div className="mt-3">
                    <div className="flex justify-between items-center mb-1">
                      <p className="text-[9px] text-neutral-500 uppercase tracking-widest">VIN_DECODED</p>
                      <p className="text-[9px] font-mono text-neutral-600">{v.vin}</p>
                    </div>
                    <div className="h-1 bg-white/5 rounded-full overflow-hidden">
                      <div className="h-full rounded-full bg-gradient-to-r from-[#00eefc] to-[#00b2bd]" style={{ width: '85%', boxShadow: '0 0 8px #00eefc' }} />
                    </div>
                  </div>
                  <p className="text-[9px] text-neutral-600 mt-2 flex items-center gap-1">
                    <span className="material-symbols-outlined text-xs">schedule</span>
                    Mise en circulation: {v.year}
                  </p>
                </div>
              </div>
            </GlassCard>
          ))}
        </div>
      )}
    </div>
  )
}

export function CataloguePage() {
  const queryClient = useQueryClient()
  const { data: stock, isLoading } = useParts(1, 25)
  const adjustStock = useAdjustStock()
  const items = stock?.items || stock || []

  const handleAdjust = async (id: string, delta: number) => {
    try {
      await adjustStock.mutateAsync({ id, delta })
      queryClient.invalidateQueries({ queryKey: ['parts'] })
    } catch (e) {
      console.error(e)
    }
  }

  return (
    <div className="space-y-8">
      <PageHeader title="CATALOGUE" accent="PIÈCES" subtitle="Inventaire en temps réel · Gestion des stocks" />
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <GlassCard className="p-4" accent="#00eefc">
          <p className="font-headline font-black text-2xl text-white">{stock?.totalCount || '---'}</p>
          <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">Références_Totales</p>
        </GlassCard>
        <GlassCard className="p-4" accent="#FF6B00">
          <p className="font-headline font-black text-2xl text-white">
            {items.filter((p: any) => p.stockQuantity <= p.minStockLevel).length || 0}
          </p>
          <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">Alerte_Stock_Bas</p>
        </GlassCard>
      </div>
      <GlassCard title="INVENTAIRE TECHNIQUE">
        <div className="overflow-x-auto">
          <table className="w-full text-left font-headline text-xs">
            <thead className="text-neutral-500 uppercase tracking-widest text-[9px]">
              <tr>
                <th className="pb-4 font-normal">Référence</th>
                <th className="pb-4 font-normal">Désignation</th>
                <th className="pb-4 font-normal">Catégorie</th>
                <th className="pb-4 font-normal">Prix HT</th>
                <th className="pb-4 font-normal">Stock</th>
                <th className="pb-4 font-normal">Statut</th>
                <th className="pb-4 font-normal text-right">Ajuster</th>
              </tr>
            </thead>
            <tbody className="text-white">
              {isLoading ? (
                <tr><td colSpan={7} className="py-8 text-center animate-pulse">Synchronisation_Flux_Données...</td></tr>
              ) : (
                items.map((p: any) => (
                  <tr key={p.id} className="border-t border-white/5 hover:bg-white/5 transition-colors group">
                    <td className="py-4 font-mono text-[#00eefc]">{p.reference}</td>
                    <td className="py-4 font-bold">{p.name}</td>
                    <td className="py-4 text-neutral-500">{p.category}</td>
                    <td className="py-4 font-mono">{(p.price || 0).toFixed(2)} €</td>
                    <td className="py-4 font-mono">
                      <span className={p.stockQuantity <= p.minStockLevel ? 'text-[#FF6B00]' : 'text-neutral-400'}>
                        {p.stockQuantity}
                      </span>
                    </td>
                    <td className="py-4">
                      <StatusBadge 
                        label={p.stockQuantity <= p.minStockLevel ? "REORDER" : "OK"} 
                        color={p.stockQuantity <= p.minStockLevel ? "#FF6B00" : "#34d399"} 
                      />
                    </td>
                    <td className="py-4 text-right flex justify-end gap-1">
                      <button 
                        onClick={() => handleAdjust(p.id, -1)}
                        disabled={adjustStock.isPending}
                        className="w-6 h-6 rounded bg-white/5 hover:bg-[#ff7351]/20 text-[#ff7351] flex items-center justify-center transition-colors disabled:opacity-50"
                      >
                        <span className="material-symbols-outlined text-sm">remove</span>
                      </button>
                      <button 
                        onClick={() => handleAdjust(p.id, 1)}
                        disabled={adjustStock.isPending}
                        className="w-6 h-6 rounded bg-white/5 hover:bg-[#00eefc]/20 text-[#00eefc] flex items-center justify-center transition-colors disabled:opacity-50"
                      >
                        <span className="material-symbols-outlined text-sm">add</span>
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </GlassCard>
    </div>
  )
}

export function GestionRhPage() {
  const staff = [
    { name: 'Marc Dubois', role: 'Chef Atelier', specialty: 'Moteurs & Transmission', tasks: 4, load: 85, available: false, color: '#FF6B00' },
    { name: 'Jean Laurent', role: 'Mécanicien', specialty: 'Freinage & Suspension', tasks: 2, load: 42, available: true, color: '#34d399' },
    { name: 'Sophie Renaud', role: 'Électronicienne', specialty: 'Diagnostic OBD & Electrique', tasks: 3, load: 60, available: true, color: '#00eefc' },
    { name: 'Pierre Moreau', role: 'Carrossier', specialty: 'Peinture & Carrosserie', tasks: 1, load: 25, available: true, color: '#a78bfa' },
  ]
  return (
    <div className="space-y-8">
      <PageHeader title="GESTION" accent="RH" subtitle="Équipe technique · Compétences · Charge de travail" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Techniciens', value: '12', color: '#00eefc', icon: 'engineering' },
          { label: 'Disponibles', value: '7', color: '#34d399', icon: 'check_circle' },
          { label: 'En intervention', value: '4', color: '#FF6B00', icon: 'build' },
          { label: 'En congé', value: '1', color: '#94a3b8', icon: 'beach_access' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="ÉQUIPE TECHNIQUE">
        <div className="space-y-4">
          {staff.map(s => (
            <div key={s.name} className="flex items-center gap-4 p-4 rounded-lg bg-white/3 border border-white/5">
              <div className="w-10 h-10 rounded-full flex items-center justify-center font-headline font-black text-sm flex-shrink-0" style={{ background: s.color + '22', color: s.color }}>
                {s.name[0]}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <p className="font-bold text-white text-xs">{s.name}</p>
                  <span className="text-[8px] px-2 py-0.5 rounded" style={{ background: s.color + '22', color: s.color }}>{s.role}</span>
                  {s.available
                    ? <span className="text-[8px] px-2 py-0.5 rounded bg-green-900/30 text-green-400">DISPO</span>
                    : <span className="text-[8px] px-2 py-0.5 rounded bg-orange-900/30 text-orange-400">OCCUPÉ</span>}
                </div>
                <p className="text-neutral-500 text-[10px] mt-0.5">{s.specialty}</p>
                <div className="flex items-center gap-3 mt-2">
                  <div className="h-1 flex-1 bg-white/5 rounded-full overflow-hidden">
                    <div className="h-full rounded-full" style={{ width: `${s.load}%`, backgroundColor: s.color, boxShadow: `0 0 6px ${s.color}` }} />
                  </div>
                  <span className="text-[9px] font-mono text-neutral-500">{s.load}% charge</span>
                </div>
              </div>
              <div className="hidden md:block text-right flex-shrink-0">
                <p className="font-mono text-[#FF6B00] text-lg font-black">{s.tasks}</p>
                <p className="text-[9px] text-neutral-600">tâches</p>
              </div>
            </div>
          ))}
        </div>
      </GlassCard>
    </div>
  )
}

export function EcommercePage() {
  return (
    <div className="space-y-8">
      <PageHeader title="E-COMMERCE" accent="BOUTIQUE" subtitle="Ventes online · Catalogue digital · Commandes web" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Commandes web', value: '34', color: '#00eefc', icon: 'shopping_cart' },
          { label: 'CA ce mois', value: '8,240 €', color: '#34d399', icon: 'trending_up' },
          { label: 'Produits actifs', value: '186', color: '#FF6B00', icon: 'inventory' },
          { label: 'Panier moyen', value: '242 €', color: '#a78bfa', icon: 'payments' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="DERNIÈRES COMMANDES WEB">
        <div className="space-y-3">
          {[
            { id: '#WEB-8821', items: 'Plaquettes Brembo × 2', amount: '178 €', status: 'Expédiée', color: '#34d399', date: '24/03' },
            { id: '#WEB-8820', items: 'Huile Castrol 5W30 × 4', amount: '168 €', status: 'En prépa', color: '#FF6B00', date: '23/03' },
            { id: '#WEB-8819', items: 'Kit distribution Bosch', amount: '215 €', status: 'Payée', color: '#00eefc', date: '22/03' },
          ].map(o => (
            <div key={o.id} className="flex items-center gap-4 p-3 rounded-lg bg-white/3 border border-white/5">
              <div>
                <p className="font-mono text-[10px] text-neutral-600">{o.id}</p>
                <p className="font-bold text-white text-xs mt-0.5">{o.items}</p>
                <p className="text-neutral-500 text-[10px]">{o.date}</p>
              </div>
              <div className="ml-auto flex items-center gap-3">
                <span className="font-mono text-[#FF6B00]">{o.amount}</span>
                <StatusBadge label={o.status} color={o.color} />
              </div>
            </div>
          ))}
        </div>
      </GlassCard>
    </div>
  )
}

export function VentesPage() {
  const [isCreating, setIsCreating] = useState(false)
  const { data: clientsData, isLoading: loadingClients } = useCustomers()
  const { data: partsData } = useParts(1, 100)
  
  const clients: any[] = Array.isArray(clientsData) ? clientsData : (clientsData?.items || [])
  const parts: any[] = Array.isArray(partsData) ? partsData : (partsData?.items || [])
  
  const [form, setForm] = useState({
    customerId: '',
    description: '',
    lines: [] as { partId: string, name: string, qty: number, price: number }[]
  })
  
  const totalHT = form.lines.reduce((acc, l) => acc + (l.qty * l.price), 0)
  const totalTTC = totalHT * 1.20

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault()
    // Simulated API call success
    alert('Devis généré avec succès pour un montant de ' + totalTTC.toLocaleString() + ' €')
    setIsCreating(false)
    setForm({ customerId: '', description: '', lines: [] })
  }

  const quotes = [
    { id: 'DEV-2025-041', client: 'Jean Durand', desc: 'Vidange + Freins complet BMW X5', amount: '480 €', expiry: '30/03', status: 'Envoyé', color: '#00eefc' },
    { id: 'DEV-2025-042', client: 'Garage Lux', desc: 'Révision 5 véhicules (lot)', amount: '3,200 €', expiry: '01/04', status: 'Accepté', color: '#34d399' },
    { id: 'DEV-2025-043', client: 'SNCF Fret', desc: 'Contrat maintenance annuel', amount: '18,000 €', expiry: '15/04', status: 'Négociation', color: '#FF6B00' },
  ]
  return (
    <div className="space-y-8">
      <div className="flex justify-between items-start flex-wrap gap-4">
        <PageHeader title="VENTES" accent="& DEVIS" subtitle="Pipeline commercial · Devis · Conversions" />
        <button
          onClick={() => setIsCreating(!isCreating)}
          className={`flex items-center gap-2 px-4 py-2 border ${isCreating ? 'border-[#ff7351] text-[#ff7351]' : 'border-[#00eefc] text-[#00eefc]'} bg-black/40 hover:bg-white/5 uppercase tracking-widest text-[9px] font-headline rounded transition-all shadow-[0_0_10px_rgba(0,238,252,0.1)]`}
        >
          <span className="material-symbols-outlined text-sm">{isCreating ? 'close' : 'add'}</span>
          {isCreating ? 'Annuler' : 'Nouveau Devis'}
        </button>
      </div>

      {isCreating && (
        <GlassCard title="ÉDITEUR DE DEVIS" className="border border-[#00eefc]/30 shadow-[0_0_20px_rgba(0,238,252,0.1)]">
          <form className="space-y-6" onSubmit={handleCreate}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Client Assigné</label>
                <select required className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none" value={form.customerId} onChange={e => setForm({...form, customerId: e.target.value})}>
                  <option value="" className="bg-[#0a0a0a]">-- Sélectionner un client --</option>
                  {clients.map((c: any) => (
                    <option key={c.id} value={c.id} className="bg-[#0a0a0a]">{c.firstName} {c.lastName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-[9px] font-headline text-neutral-500 uppercase tracking-widest mb-1">Motif / Description courte</label>
                <input required className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-[#00eefc]/50 outline-none" value={form.description} onChange={e => setForm({...form, description: e.target.value})} placeholder="Ex: Révision des 100k km" />
              </div>
            </div>

            <div className="border border-white/5 rounded p-4 bg-black/20">
              <div className="flex justify-between items-center mb-4">
                <p className="font-headline font-bold text-xs uppercase tracking-widest text-white">Lignes de prestation</p>
                <button 
                  type="button" 
                  onClick={() => setForm({...form, lines: [...form.lines, { partId: '', name: '', qty: 1, price: 0 }]})}
                  className="text-[9px] uppercase tracking-widest text-[#00eefc] hover:text-white flex items-center gap-1"
                >
                  <span className="material-symbols-outlined text-[12px]">add</span> Ajouter Vierge
                </button>
              </div>
              
              <div className="space-y-2">
                {form.lines.map((l, i) => (
                  <div key={i} className="flex gap-2 items-center">
                    <select 
                      className="flex-1 bg-white/5 border border-white/10 rounded px-2 py-1 text-xs text-white"
                      value={l.partId}
                      onChange={(e) => {
                        const p = parts.find((x: any) => x.id === e.target.value)
                        const newLines = [...form.lines]
                        newLines[i] = { partId: e.target.value, name: p ? p.name : '', qty: l.qty, price: p ? p.price : 0 }
                        setForm({...form, lines: newLines})
                      }}
                    >
                      <option value="" className="bg-[#0a0a0a]">- Prestation ou Pièce -</option>
                      {parts.map((p: any) => (
                        <option key={p.id} value={p.id} className="bg-[#0a0a0a]">{p.reference} : {p.name}</option>
                      ))}
                    </select>
                    <input type="number" min="1" className="w-20 bg-white/5 border border-white/10 rounded px-2 py-1 text-xs text-white text-center" value={l.qty} onChange={(e) => {
                      const newLines = [...form.lines]
                      newLines[i].qty = parseInt(e.target.value) || 1
                      setForm({...form, lines: newLines})
                    }} />
                    <div className="w-24 text-right font-mono text-[10px] text-neutral-400">
                      ={(l.qty * l.price).toLocaleString()} €
                    </div>
                    <button type="button" onClick={() => setForm(f => ({...f, lines: f.lines.filter((_, idx) => idx !== i)}))} className="text-[#ff7351] hover:text-red-400 material-symbols-outlined text-sm ml-2">
                      delete
                    </button>
                  </div>
                ))}
                {form.lines.length === 0 && <p className="text-center text-[10px] text-neutral-500 py-4 font-mono">PANIER_VIDE</p>}
              </div>

              <div className="mt-4 pt-4 border-t border-white/5 flex justify-between items-center bg-white/5 p-4 rounded">
                 <p className="text-[10px] text-neutral-500 uppercase tracking-widest font-headline">Estimation Totale (TTC 20%)</p>
                 <p className="font-mono text-xl font-bold text-[#00eefc]">{totalTTC.toLocaleString()} €</p>
              </div>
            </div>

            <div className="flex justify-end">
              <button 
                type="submit"
                className="bg-[#00eefc] text-black px-8 py-2 rounded font-headline font-black text-[10px] tracking-widest uppercase shadow-[0_0_10px_rgba(0,238,252,0.3)] hover:shadow-[0_0_20px_rgba(0,238,252,0.5)] transition-all"
              >
                Générer & Transmettre
              </button>
            </div>
          </form>
        </GlassCard>
      )}

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Devis en cours', value: '18', color: '#00eefc', icon: 'description' },
          { label: 'Taux conversion', value: '68%', color: '#34d399', icon: 'percent' },
          { label: 'Pipeline total', value: '42,800 €', color: '#FF6B00', icon: 'analytics' },
          { label: 'Délai moy. réponse', value: '2.4j', color: '#a78bfa', icon: 'schedule' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="DEVIS ACTIFS">
        <div className="overflow-x-auto -mx-6 px-6">
          <table className="w-full text-sm min-w-[600px]">
            <thead>
              <tr className="text-[9px] font-headline uppercase tracking-widest text-neutral-600 border-b border-white/5">
                <th className="py-3 px-4 text-left">Réf.</th>
                <th className="py-3 px-4 text-left">Client</th>
                <th className="py-3 px-4 text-left">Description</th>
                <th className="py-3 px-4 text-left">Montant</th>
                <th className="py-3 px-4 text-left">Expiration</th>
                <th className="py-3 px-4 text-right">Statut</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5">
              {quotes.map(q => (
                <tr key={q.id} className="hover:bg-white/5 transition-all">
                  <td className="py-4 px-4 font-mono text-[10px] text-neutral-600">{q.id}</td>
                  <td className="py-4 px-4 font-bold text-white text-xs">{q.client}</td>
                  <td className="py-4 px-4 text-neutral-400 text-xs">{q.desc}</td>
                  <td className="py-4 px-4 font-mono text-[#FF6B00]">{q.amount}</td>
                  <td className="py-4 px-4 font-mono text-[10px] text-neutral-500">{q.expiry}</td>
                  <td className="py-4 px-4 text-right"><StatusBadge label={q.status} color={q.color} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </GlassCard>
    </div>
  )
}

export function CommandesPage() {
  const orders = [
    { id: 'CMD-F-2025-011', supplier: 'Brembo France', items: 'Plaquettes AX5 × 12', amount: '1,068 €', eta: '26/03', status: 'En transit', color: '#00eefc' },
    { id: 'CMD-F-2025-012', supplier: 'Castrol PRO', items: 'Huile 5W30 × 50L', amount: '420 €', eta: '27/03', status: 'Confirmée', color: '#34d399' },
    { id: 'CMD-F-2025-013', supplier: 'Bosch Pièces', items: 'Kits distribution × 5', amount: '650 €', eta: '28/03', status: 'En attente', color: '#FF6B00' },
  ]
  return (
    <div className="space-y-8">
      <PageHeader title="COMMANDES" accent="FOURNISSEURS" subtitle="Approvisionnements · Délais livraison · Réceptions" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'En transit', value: '8', color: '#00eefc', icon: 'local_shipping' },
          { label: 'Confirmées', value: '5', color: '#34d399', icon: 'check_circle' },
          { label: 'En attente', value: '3', color: '#FF6B00', icon: 'hourglass_empty' },
          { label: 'Valeur totale', value: '9,200 €', color: '#a78bfa', icon: 'euro' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-2xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="COMMANDES EN COURS">
        <div className="overflow-x-auto -mx-6 px-6">
          <table className="w-full text-sm min-w-[600px]">
            <thead>
              <tr className="text-[9px] font-headline uppercase tracking-widest text-neutral-600 border-b border-white/5">
                <th className="py-3 px-4 text-left">ID</th>
                <th className="py-3 px-4 text-left">Fournisseur</th>
                <th className="py-3 px-4 text-left">Articles</th>
                <th className="py-3 px-4 text-left">Montant</th>
                <th className="py-3 px-4 text-left">Livraison</th>
                <th className="py-3 px-4 text-right">Statut</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5">
              {orders.map(o => (
                <tr key={o.id} className="hover:bg-white/5 transition-all">
                  <td className="py-4 px-4 font-mono text-[10px] text-neutral-600">{o.id}</td>
                  <td className="py-4 px-4 font-bold text-white text-xs">{o.supplier}</td>
                  <td className="py-4 px-4 text-neutral-400 text-xs">{o.items}</td>
                  <td className="py-4 px-4 font-mono text-[#FF6B00]">{o.amount}</td>
                  <td className="py-4 px-4 font-mono text-[10px] text-neutral-500">{o.eta}</td>
                  <td className="py-4 px-4 text-right"><StatusBadge label={o.status} color={o.color} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </GlassCard>
    </div>
  )
}

export function SuiviExpeditionsPage() {
  const shipments = [
    { id: 'EXP-001', origin: 'Atelier Principal', dest: 'Jean Durand — 12 rue de la Paix', items: 'Plaquettes montées + rapport', carrier: 'DPD', eta: 'Aujourd\'hui 16h', status: 'En livraison', color: '#34d399', progress: 85 },
    { id: 'EXP-002', origin: 'Fournisseur Brembo Lyon', dest: 'Atelier MecaPro', items: 'Lot plaquettes AX5 × 12', carrier: 'Fedex', eta: '26/03 matin', status: 'En transit', color: '#00eefc', progress: 50 },
    { id: 'EXP-003', origin: 'Bosch Entrepôt', dest: 'Atelier MecaPro', items: 'Kits distribution × 5', carrier: 'UPS', eta: '28/03', status: 'Expédiée', color: '#FF6B00', progress: 20 },
  ]
  return (
    <div className="space-y-8">
      <PageHeader title="SUIVI" accent="EXPÉDITIONS" subtitle="Tracking temps réel · Livraisons · Réceptions" />
      <div className="space-y-4">
        {shipments.map(s => (
          <GlassCard key={s.id}>
            <div className="flex flex-col md:flex-row md:items-center gap-4">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2 flex-wrap">
                  <span className="font-mono text-[10px] text-neutral-600">{s.id}</span>
                  <StatusBadge label={s.status} color={s.color} />
                  <span className="text-[10px] text-neutral-600">{s.carrier}</span>
                </div>
                <p className="font-bold text-white text-sm">{s.items}</p>
                <div className="flex items-center gap-2 mt-1 text-[10px] text-neutral-500">
                  <span>{s.origin}</span>
                  <span className="material-symbols-outlined text-xs">arrow_forward</span>
                  <span>{s.dest}</span>
                </div>
              </div>
              <div className="md:text-right flex-shrink-0">
                <p className="text-[10px] text-neutral-500 mb-2">ETA: <span style={{ color: s.color }}>{s.eta}</span></p>
                <div className="h-1.5 w-full md:w-48 bg-white/5 rounded-full overflow-hidden">
                  <div className="h-full rounded-full transition-all" style={{ width: `${s.progress}%`, backgroundColor: s.color, boxShadow: `0 0 8px ${s.color}` }} />
                </div>
                <p className="text-[9px] text-neutral-600 mt-1">{s.progress}% acheminé</p>
              </div>
            </div>
          </GlassCard>
        ))}
      </div>
    </div>
  )
}

export function TresoreriePage() {
  return (
    <div className="space-y-8">
      <PageHeader title="TRÉSORERIE" accent="& FINANCE" subtitle="Soldes · Flux de trésorerie · Prévisions" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Solde compte principal', value: '48,320 €', color: '#34d399', icon: 'account_balance' },
          { label: 'Encaissements à venir', value: '12,450 €', color: '#00eefc', icon: 'trending_up' },
          { label: 'Décaissements prévus', value: '6,800 €', color: '#FF6B00', icon: 'trending_down' },
          { label: 'Solde prévisionnel J+30', value: '54,000 €', color: '#a78bfa', icon: 'savings' },
        ].map(k => (
          <GlassCard key={k.label} className="p-4">
            <span className="material-symbols-outlined mb-2 block" style={{ color: k.color }}>{k.icon}</span>
            <p className="font-headline font-black text-xl text-white">{k.value}</p>
            <p className="text-[9px] font-headline uppercase tracking-widest text-neutral-500">{k.label}</p>
          </GlassCard>
        ))}
      </div>
      <GlassCard title="FLUX RÉCENTS">
        <div className="space-y-3">
          {[
            { label: 'Paiement client Jean Durand', amount: '+1,240 €', date: '23/03', type: 'IN', color: '#34d399' },
            { label: 'Fournisseur Brembo — CMD-011', amount: '-1,068 €', date: '22/03', type: 'OUT', color: '#ff7351' },
            { label: 'Loyer atelier Mars', amount: '-2,400 €', date: '20/03', type: 'OUT', color: '#ff7351' },
            { label: 'Paiement SNCF Fret', amount: '+4,560 €', date: '19/03', type: 'IN', color: '#34d399' },
          ].map((f, i) => (
            <div key={i} className="flex items-center gap-4 p-3 rounded-lg bg-white/3 border border-white/5">
              <span className="text-xs w-8 text-center font-black" style={{ color: f.color }}>{f.type === 'IN' ? '▲' : '▼'}</span>
              <div className="flex-1">
                <p className="text-white text-xs font-bold">{f.label}</p>
                <p className="text-neutral-600 text-[10px]">{f.date}</p>
              </div>
              <span className="font-mono" style={{ color: f.color }}>{f.amount}</span>
            </div>
          ))}
        </div>
      </GlassCard>
    </div>
  )
}

export function PlanningStaffPage() {
  const days = ['LUN 24', 'MAR 25', 'MER 26', 'JEU 27', 'VEN 28']
  const staff = [
    { name: 'Marc D.', slots: ['Vidange BMW', 'Freins C3', '', 'Révision Clio', 'Repos'] },
    { name: 'Jean L.', slots: ['', 'Pneus × 4', 'OBD Scan', '', 'Climatisation'] },
    { name: 'Sophie R.', slots: ['Diagnostic', '', 'Câblage VW', 'OBD Scan', 'Diagnostic'] },
    { name: 'Pierre M.', slots: ['Carrosserie', 'Peinture', 'Peinture', '', 'Carrosserie'] },
  ]
  return (
    <div className="space-y-8">
      <PageHeader title="PLANNING" accent="STAFF" subtitle="Agenda techniciens · Affectations · Absences" />
      <GlassCard title="SEMAINE EN COURS">
        <div className="overflow-x-auto -mx-6 px-6">
          <table className="w-full text-xs min-w-[600px]">
            <thead>
              <tr className="border-b border-white/5">
                <th className="py-3 px-4 text-left text-[9px] font-headline uppercase tracking-widest text-neutral-600">Technicien</th>
                {days.map(d => (
                  <th key={d} className="py-3 px-4 text-center text-[9px] font-headline uppercase tracking-widest text-neutral-600">{d}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5">
              {staff.map(s => (
                <tr key={s.name} className="hover:bg-white/3">
                  <td className="py-3 px-4 font-bold text-white">{s.name}</td>
                  {s.slots.map((slot, i) => (
                    <td key={i} className="py-3 px-4 text-center">
                      {slot
                        ? <span className="text-[9px] px-2 py-1 rounded bg-[#FF6B00]/10 text-[#FF6B00] border border-[#FF6B00]/20 whitespace-nowrap">{slot}</span>
                        : <span className="text-neutral-800 text-[10px]">—</span>
                      }
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </GlassCard>
    </div>
  )
}

export function ParametresPage() {
  return (
    <div className="space-y-8">
      <PageHeader title="PARAMÈTRES" accent="SYSTÈME" subtitle="Configuration · Préférences · Sécurité" />
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {[
          { title: 'Informations Atelier', icon: 'store', fields: ['Nom: MecaPro Garage', 'Adresse: 14 av. de l\'Industrie', 'SIRET: 814 201 234 00012', 'TVA: FR 12 814201234'] },
          { title: 'Notifications', icon: 'notifications', fields: ['Email alertes: oui', 'SMS urgences: oui', 'Rapport hebdo: dimanche', 'Push mobile: activé'] },
          { title: 'Intégrations', icon: 'integration_instructions', fields: ['API C# Backend: connecté', 'RabbitMQ: optionnel', 'Redis Cache: actif', 'SQL Server: connecté'] },
          { title: 'Sécurité', icon: 'security', fields: ['Auth JWT: activé', '2FA: désactivé', 'Session: 8h', 'Logs: conservés 90j'] },
        ].map(section => (
          <GlassCard key={section.title}>
            <div className="flex items-center gap-3 mb-4">
              <span className="material-symbols-outlined text-[#FF6B00]">{section.icon}</span>
              <h3 className="font-headline font-bold text-white text-sm uppercase tracking-widest">{section.title}</h3>
            </div>
            <div className="space-y-2">
              {section.fields.map((f, i) => (
                <div key={i} className="flex items-center gap-2 p-2 rounded bg-white/3 border border-white/5">
                  <span className="text-neutral-600 text-[10px] font-mono">{f}</span>
                </div>
              ))}
            </div>
          </GlassCard>
        ))}
      </div>
    </div>
  )
}
