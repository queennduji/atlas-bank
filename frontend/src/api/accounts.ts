import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { Account, CreateAccountRequest } from './types';

export const accountsApi = {
  myAccounts: () => api.get<Account[]>('/api/accounts/me'),
  byId: (id: string) => api.get<Account>(`/api/accounts/${id}`),
  create: (body: CreateAccountRequest) => api.post<Account>('/api/accounts', body),
};

export function useMyAccounts(enabled: boolean) {
  return useQuery({
    queryKey: ['accounts', 'me'],
    queryFn: accountsApi.myAccounts,
    enabled,
  });
}

export function useAccount(id: string | undefined) {
  return useQuery({
    queryKey: ['accounts', id],
    queryFn: () => accountsApi.byId(id!),
    enabled: !!id,
  });
}

export function useCreateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: accountsApi.create,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['accounts', 'me'] }),
  });
}
