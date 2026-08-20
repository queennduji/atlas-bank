import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { DepositRequest, Transaction, TransferRequest, WithdrawRequest } from './types';

export const transactionsApi = {
  byAccount: (accountId: string) => api.get<Transaction[]>(`/api/transactions/account/${accountId}`),
  byId: (id: string) => api.get<Transaction>(`/api/transactions/${id}`),
  deposit: (body: DepositRequest) => api.post<Transaction>('/api/transactions/deposit', body),
  withdraw: (body: WithdrawRequest) => api.post<Transaction>('/api/transactions/withdraw', body),
  transfer: (body: TransferRequest) => api.post<Transaction>('/api/transactions/transfer', body),
};

export function useAccountTransactions(accountId: string | undefined) {
  return useQuery({
    queryKey: ['transactions', 'account', accountId],
    queryFn: () => transactionsApi.byAccount(accountId!),
    enabled: !!accountId,
  });
}

function useInvalidateAfterMoneyMovement() {
  const queryClient = useQueryClient();
  return (accountIds: (string | undefined)[]) => {
    queryClient.invalidateQueries({ queryKey: ['accounts', 'me'] });
    for (const id of accountIds) {
      if (!id) continue;
      queryClient.invalidateQueries({ queryKey: ['accounts', id] });
      queryClient.invalidateQueries({ queryKey: ['transactions', 'account', id] });
    }
  };
}

export function useDeposit() {
  const invalidate = useInvalidateAfterMoneyMovement();
  return useMutation({
    mutationFn: transactionsApi.deposit,
    onSuccess: (tx) => invalidate([tx.accountId]),
  });
}

export function useWithdraw() {
  const invalidate = useInvalidateAfterMoneyMovement();
  return useMutation({
    mutationFn: transactionsApi.withdraw,
    onSuccess: (tx) => invalidate([tx.accountId]),
  });
}

export function useTransfer() {
  const invalidate = useInvalidateAfterMoneyMovement();
  return useMutation({
    mutationFn: transactionsApi.transfer,
    onSuccess: (tx) => invalidate([tx.accountId, tx.toAccountId ?? undefined]),
  });
}
