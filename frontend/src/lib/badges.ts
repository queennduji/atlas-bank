import type { AccountStatusValue, CardStatus, TransactionStatusValue } from '@/api/types';
import { AccountStatusLabel, TransactionStatusLabel } from '@/api/types';

type Tone = 'positive' | 'negative' | 'warning' | 'neutral' | 'brand';

export function accountStatusBadge(status: AccountStatusValue): { label: string; tone: Tone } {
  const label = AccountStatusLabel[status];
  const tone: Tone = status === 0 ? 'positive' : status === 1 ? 'warning' : 'neutral';
  return { label, tone };
}

export function transactionStatusBadge(status: TransactionStatusValue): { label: string; tone: Tone } {
  const label = TransactionStatusLabel[status];
  const tone: Tone = status === 1 ? 'positive' : status === 0 ? 'warning' : 'negative';
  return { label, tone };
}

export function cardStatusBadge(status: CardStatus): { label: string; tone: Tone } {
  const tone: Tone = status === 'Active' ? 'positive' : status === 'Frozen' ? 'warning' : 'neutral';
  return { label: status, tone };
}
