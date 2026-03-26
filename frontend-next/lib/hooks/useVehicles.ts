import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'

export interface Vehicle {
  id: string
  vin: string
  licensePlate: string
  brand: string
  model: string
  year: number
  mileage: number
  customerId: string
  customerName?: string
}

export interface CreateVehicleDto {
  vin: string
  licensePlate: string
  brand: string
  model: string
  year: number
  mileage: number
  customerId: string
}

export function useVehicles(page = 1, pageSize = 10, search = '') {
  return useQuery({
    queryKey: ['vehicles', page, pageSize, search],
    queryFn: async () => {
      const res = await api.get<any>('/api/v1/vehicles', {
        params: { page, pageSize, search }
      })
      return res.data
    }
  })
}

export function useCreateVehicle() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (vehicle: CreateVehicleDto) => {
      const res = await api.post<Vehicle>('/api/v1/vehicles', vehicle)
      return res.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicles'] })
    }
  })
}
