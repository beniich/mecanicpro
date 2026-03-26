import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'

export interface Invoice {
  id: string
  invoiceNumber: string
  issuedAt: string
  dueDate: string
  totalHT: number
  totalTTC: number
  status: 'Draft' | 'Sent' | 'Paid' | 'Overdue' | 'Cancelled'
  customerId: string
  customerName?: string
}

export function useInvoices() {
  return useQuery({
    queryKey: ['invoices'],
    queryFn: async () => {
      const res = await api.get<Invoice[]>('/api/v1/billing/invoices')
      return res.data
    }
  })
}
