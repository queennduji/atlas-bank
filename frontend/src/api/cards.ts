import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { Card, IssueCardRequest, UpdateSpendingLimitRequest } from './types';

export const cardsApi = {
  byAccount: (accountId: string) => api.get<Card[]>(`/api/cards/account/${accountId}`),
  byId: (id: string) => api.get<Card>(`/api/cards/${id}`),
  issue: (body: IssueCardRequest) => api.post<Card>('/api/cards', body),
  freeze: (id: string) => api.post<Card>(`/api/cards/${id}/freeze`),
  unfreeze: (id: string) => api.post<Card>(`/api/cards/${id}/unfreeze`),
  updateSpendingLimit: (id: string, body: UpdateSpendingLimitRequest) =>
    api.put<Card>(`/api/cards/${id}/spendingLimit`, body),
};

export function useAccountCards(accountId: string | undefined) {
  return useQuery({
    queryKey: ['cards', 'account', accountId],
    queryFn: () => cardsApi.byAccount(accountId!),
    enabled: !!accountId,
  });
}

function useInvalidateCards() {
  const queryClient = useQueryClient();
  return (accountId: string) => queryClient.invalidateQueries({ queryKey: ['cards', 'account', accountId] });
}

export function useIssueCard() {
  const invalidate = useInvalidateCards();
  return useMutation({
    mutationFn: cardsApi.issue,
    onSuccess: (card) => invalidate(card.accountId),
  });
}

export function useFreezeCard() {
  const invalidate = useInvalidateCards();
  return useMutation({
    mutationFn: cardsApi.freeze,
    onSuccess: (card) => invalidate(card.accountId),
  });
}

export function useUnfreezeCard() {
  const invalidate = useInvalidateCards();
  return useMutation({
    mutationFn: cardsApi.unfreeze,
    onSuccess: (card) => invalidate(card.accountId),
  });
}

export function useUpdateSpendingLimit() {
  const invalidate = useInvalidateCards();
  return useMutation({
    mutationFn: ({ id, spendingLimit }: { id: string; spendingLimit: number }) =>
      cardsApi.updateSpendingLimit(id, { spendingLimit }),
    onSuccess: (card) => invalidate(card.accountId),
  });
}
