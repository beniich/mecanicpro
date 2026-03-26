import { useQuery, useMutation } from '@tanstack/react-query'
import api from '@/lib/api'

export interface Part {
  id: string
  reference: string
  name: string
  category: string
  price: number
  stockQuantity: number
  minStockLevel: number
}

export function useParts(page = 1, pageSize = 20, search = '', category = '') {
  return useQuery({
    queryKey: ['parts', page, pageSize, search, category],
    queryFn: async () => {
      const res = await api.get<any>('/api/v1/parts', {
        params: { page, pageSize, search, category }
      })
      return res.data
    }
  })
}

export function usePartCategories() {
  return useQuery({
    queryKey: ['part-categories'],
    queryFn: async () => {
      const res = await api.get<string[]>('/api/v1/parts/categories')
      return res.data
    }
  })
}

export function useAdjustStock() {
  return useMutation({
    mutationFn: async ({ id, delta }: { id: string, delta: number }) => {
      const res = await api.post(`/api/v1/parts/${id}/stock`, { id, delta })
      return res.data
    }
  })
}
