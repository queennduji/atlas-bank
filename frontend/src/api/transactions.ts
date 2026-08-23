import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { DepositRequest, Transaction, TransferRequest, WithdrawRequest } from './types';

// Money-moving requests carry an Idempotency-Key so a request that's retried
// (a client-side retry, or a user resubmitting after a response was lost to a
// network blip) doesn't create a second transaction — the backend returns the
// original result for a repeated key instead of processing it again. Callers
// that implement their own retry logic should generate the key once and pass
// it through explicitly so every attempt of the same logical request reuses
// it; one-shot callers can rely on the default of a fresh key per call.
export const transactionsApi = {
  byAccount: (accountId: string) => api.get<Transaction[]>(`/api/transactions/account/${accountId}`),
  byId: (id: string) => api.get<Transaction>(`/api/transactions/${id}`),
  deposit: (body: DepositRequest, idempotencyKey = crypto.randomUUID()) =>
    api.post<Transaction>('/api/transactions/deposit', body, { 'Idempotency-Key': idempotencyKey }),
  withdraw: (body: WithdrawRequest, idempotencyKey = crypto.randomUUID()) =>
    api.post<Transaction>('/api/transactions/withdraw', body, { 'Idempotency-Key': idempotencyKey }),
  transfer: (body: TransferRequest, idempotencyKey = crypto.randomUUID()) =>
    api.post<Transaction>('/api/transactions/transfer', body, { 'Idempotency-Key': idempotencyKey }),
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
    // Wrapped rather than passed directly: TanStack Query calls mutationFn as
    // (variables, context), and that context object would otherwise land in
    // transactionsApi.deposit's second (idempotencyKey) parameter position.
    mutationFn: (body: DepositRequest) => transactionsApi.deposit(body),
    onSuccess: (tx) => invalidate([tx.accountId]),
  });
}

export function useWithdraw() {
  const invalidate = useInvalidateAfterMoneyMovement();
  return useMutation({
    mutationFn: (body: WithdrawRequest) => transactionsApi.withdraw(body),
    onSuccess: (tx) => invalidate([tx.accountId]),
  });
}

export function useTransfer() {
  const invalidate = useInvalidateAfterMoneyMovement();
  return useMutation({
    mutationFn: (body: TransferRequest) => transactionsApi.transfer(body),
    onSuccess: (tx) => invalidate([tx.accountId, tx.toAccountId ?? undefined]),
  });
}
