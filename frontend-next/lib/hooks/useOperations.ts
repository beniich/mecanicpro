import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'

export interface Revision {
  id: string
  scheduledDate: string
  description: string
  status: 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled'
  vehicleId: string
  vehicleName?: string
  mechanicId?: string
  mechanicName?: string
}

export interface PagedResult<T> {
  items: T[]
  pageIndex: number
  totalPages: number
  totalCount: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export function useRevisions(page = 1, pageSize = 20, search = '') {
  return useQuery({
    queryKey: ['revisions', page, pageSize, search],
    queryFn: async () => {
      const res = await api.get<PagedResult<Revision>>('/api/v1/revisions', {
        params: { page, pageSize, search }
      })
      return res.data
    }
  })
}

export function useWorkshopSchedule(start: string, end: string) {
  return useQuery({
    queryKey: ['revisions', 'schedule', start, end],
    queryFn: async () => {
      const res = await api.get<Revision[]>('/api/v1/revisions/schedule', {
        params: { start, end }
      })
      return res.data
    }
  })
}
