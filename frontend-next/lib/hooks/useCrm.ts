import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'

export interface Customer {
  id: string
  firstName: string
  lastName: string
  email: string
  phoneNumber?: string
  loyaltyPoints: number
  lastVisit?: string
}

export interface CreateCustomerDto {
  firstName: string
  lastName: string
  email: string
  phoneNumber?: string
}

export interface PagedResult<T> {
  items: T[]
  pageIndex: number
  totalPages: number
  totalCount: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export function useCustomers(page = 1, pageSize = 10, search = '') {
  return useQuery({
    queryKey: ['customers', page, pageSize, search],
    queryFn: async () => {
      const res = await api.get<PagedResult<Customer>>('/api/v1/customers', {
        params: { page, pageSize, search }
      })
      return res.data
    }
  })
}

export function useCreateCustomer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (customer: CreateCustomerDto) => {
      const res = await api.post<Customer>('/api/v1/customers', customer)
      return res.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] })
    }
  })
}
