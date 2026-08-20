import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { GenerateStatementRequest, Statement, StatementSummary } from './types';

export const statementsApi = {
  byAccount: (accountId: string) => api.get<StatementSummary[]>(`/api/statements/account/${accountId}`),
  byId: (id: string) => api.get<Statement>(`/api/statements/${id}`),
  generate: (body: GenerateStatementRequest) => api.post<Statement>('/api/statements/generate', body),
};

export function useAccountStatements(accountId: string | undefined) {
  return useQuery({
    queryKey: ['statements', 'account', accountId],
    queryFn: () => statementsApi.byAccount(accountId!),
    enabled: !!accountId,
  });
}

export function useStatement(id: string | undefined) {
  return useQuery({
    queryKey: ['statements', id],
    queryFn: () => statementsApi.byId(id!),
    enabled: !!id,
  });
}

export function useGenerateStatement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: statementsApi.generate,
    onSuccess: (statement) =>
      queryClient.invalidateQueries({ queryKey: ['statements', 'account', statement.accountId] }),
  });
}
