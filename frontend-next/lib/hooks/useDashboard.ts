import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'

export interface DashboardStats {
  vehiclesInProgress: number
  activeDiagnostics: number
  totalClients: number
  todayRevisions: number
}

export interface FinanceMetrics {
  monthlyRevenue: number
  pendingRevenue: number
  growthPercentage: number
  totalInvoices: number
}

export function useDashboardStats() {
  return useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: async () => {
      const res = await api.get<DashboardStats>('/api/v1/dashboard/stats')
      return res.data
    },
    refetchInterval: 30000, // Refresh every 30s
  })
}

export function useFinanceMetrics() {
  return useQuery({
    queryKey: ['finance-metrics'],
    queryFn: async () => {
      const res = await api.get<FinanceMetrics>('/api/v1/dashboard/finance-metrics')
      return res.data
    },
    refetchInterval: 60000,
  })
}
