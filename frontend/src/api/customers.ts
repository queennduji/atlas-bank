import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { Customer, RegisterCustomerRequest, UpdateCustomerRequest } from './types';

export const customersApi = {
  register: (body: RegisterCustomerRequest) => api.post<Customer>('/api/customers/register', body),
  me: () => api.get<Customer>('/api/customers/me'),
  updateMe: (body: UpdateCustomerRequest) => api.put<Customer>('/api/customers/me', body),
};

export function useMe(enabled: boolean) {
  return useQuery({
    queryKey: ['customer', 'me'],
    queryFn: customersApi.me,
    enabled,
    retry: false,
  });
}

export function useRegisterCustomer() {
  return useMutation({ mutationFn: customersApi.register });
}

export function useUpdateMe() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: customersApi.updateMe,
    onSuccess: (customer) => queryClient.setQueryData(['customer', 'me'], customer),
  });
}
