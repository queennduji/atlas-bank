import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import { AccountType, type Account, type CreateAccountRequest } from './types';

export const accountsApi = {
  myAccounts: () => api.get<Account[]>('/api/accounts/me'),
  byId: (id: string) => api.get<Account>(`/api/accounts/${id}`),
  create: (body: CreateAccountRequest) => api.post<Account>('/api/accounts', body),
};

/**
 * Account the cards and statements pages open on. Checking is the everyday account (and
 * the one the seeded demo data hangs cards and statements off), and the API doesn't
 * guarantee any ordering, so prefer it over whichever account happens to come first.
 */
export function defaultAccountId(accounts: Account[] | undefined): string | undefined {
  return (accounts?.find((a) => a.type === AccountType.Checking) ?? accounts?.[0])?.id;
}

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
