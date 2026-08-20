import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useSearchParams } from 'react-router-dom';
import { CreditCard, Plus, Snowflake, Sun } from 'lucide-react';
import { useMyAccounts } from '@/api/accounts';
import { useAccountCards, useFreezeCard, useIssueCard, useUnfreezeCard, useUpdateSpendingLimit } from '@/api/cards';
import { AccountTypeLabel, type CardType } from '@/api/types';
import { Card } from '@/components/ui/Card';
import { Select } from '@/components/ui/Select';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';
import { Spinner } from '@/components/ui/Spinner';
import { EmptyState } from '@/components/ui/EmptyState';
import { useToast } from '@/components/ui/Toast';
import { ApiError } from '@/api/client';
import { formatDate, formatMoney } from '@/lib/format';
import { cardStatusBadge } from '@/lib/badges';

const issueSchema = z.object({
  type: z.enum(['Debit', 'Credit']),
  spendingLimit: z.string().refine((v) => Number(v) > 0, 'Enter a limit greater than 0'),
});
type IssueForm = z.infer<typeof issueSchema>;

export function Cards() {
  const [params] = useSearchParams();
  const { data: accounts } = useMyAccounts(true);
  const [accountId, setAccountId] = useState(params.get('accountId') ?? '');
  const activeAccountId = accountId || accounts?.[0]?.id;

  const { data: cards, isLoading } = useAccountCards(activeAccountId);
  const issueCard = useIssueCard();
  const freeze = useFreezeCard();
  const unfreeze = useUnfreezeCard();
  const updateLimit = useUpdateSpendingLimit();
  const toast = useToast();

  const [issueOpen, setIssueOpen] = useState(false);
  const [limitCardId, setLimitCardId] = useState<string | null>(null);
  const [limitValue, setLimitValue] = useState('');

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<IssueForm>({ resolver: zodResolver(issueSchema), defaultValues: { type: 'Debit' } });

  async function onIssue(values: IssueForm) {
    if (!activeAccountId) return;
    try {
      await issueCard.mutateAsync({
        accountId: activeAccountId,
        type: values.type as CardType,
        spendingLimit: Number(values.spendingLimit),
      });
      toast.success('Card issued.');
      setIssueOpen(false);
      reset();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Could not issue card.');
    }
  }

  async function toggleFreeze(cardId: string, frozen: boolean) {
    try {
      if (frozen) await unfreeze.mutateAsync(cardId);
      else await freeze.mutateAsync(cardId);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Could not update card.');
    }
  }

  async function saveLimit(cardId: string) {
    const amount = Number(limitValue);
    if (!Number.isFinite(amount) || amount <= 0) {
      toast.error('Enter a valid limit.');
      return;
    }
    try {
      await updateLimit.mutateAsync({ id: cardId, spendingLimit: amount });
      toast.success('Spending limit updated.');
      setLimitCardId(null);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Could not update limit.');
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-(--color-text)">Cards</h1>
          <p className="mt-0.5 text-sm text-(--color-text-muted)">Issue and manage cards linked to your accounts.</p>
        </div>
        <div className="flex items-center gap-2">
          <Select value={accountId || activeAccountId || ''} onChange={(e) => setAccountId(e.target.value)}>
            {accounts?.map((a) => (
              <option key={a.id} value={a.id}>
                {AccountTypeLabel[a.type]} · {a.accountNumber}
              </option>
            ))}
          </Select>
          <Button icon={<Plus size={16} />} onClick={() => setIssueOpen(true)} disabled={!activeAccountId}>
            Issue card
          </Button>
        </div>
      </div>

      <Card>
        {isLoading ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : !cards || cards.length === 0 ? (
          <EmptyState
            icon={<CreditCard size={28} />}
            title="No cards on this account"
            description="Issue a debit or credit card to start spending from this account."
            action={
              <Button onClick={() => setIssueOpen(true)} disabled={!activeAccountId}>
                Issue card
              </Button>
            }
          />
        ) : (
          <div className="divide-y divide-(--color-border)">
            {cards.map((card) => {
              const status = cardStatusBadge(card.status);
              const frozen = card.status === 'Frozen';
              return (
                <div key={card.id} className="flex flex-wrap items-center justify-between gap-3 px-5 py-4">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-14 items-center justify-center rounded-md bg-(--color-brand-soft) text-(--color-brand)">
                      <CreditCard size={18} />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-(--color-text)">
                        {card.type} · {card.maskedCardNumber}
                      </p>
                      <p className="text-xs text-(--color-text-muted)">
                        {card.cardHolderName} · expires {formatDate(card.expiryDate)}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    {limitCardId === card.id ? (
                      <div className="flex items-center gap-1.5">
                        <Input
                          className="h-8 w-28"
                          type="number"
                          step="0.01"
                          value={limitValue}
                          onChange={(e) => setLimitValue(e.target.value)}
                          autoFocus
                        />
                        <Button size="sm" onClick={() => saveLimit(card.id)} loading={updateLimit.isPending}>
                          Save
                        </Button>
                        <Button size="sm" variant="ghost" onClick={() => setLimitCardId(null)}>
                          Cancel
                        </Button>
                      </div>
                    ) : (
                      <button
                        onClick={() => {
                          setLimitCardId(card.id);
                          setLimitValue(String(card.spendingLimit));
                        }}
                        className="text-sm text-(--color-text-muted) underline decoration-dotted hover:text-(--color-text)"
                      >
                        Limit: {formatMoney(card.spendingLimit)}
                      </button>
                    )}

                    <Badge tone={status.tone}>{status.label}</Badge>

                    {card.status !== 'Cancelled' && card.status !== 'Expired' && (
                      <Button
                        size="sm"
                        variant="secondary"
                        icon={frozen ? <Sun size={14} /> : <Snowflake size={14} />}
                        loading={freeze.isPending || unfreeze.isPending}
                        onClick={() => toggleFreeze(card.id, frozen)}
                      >
                        {frozen ? 'Unfreeze' : 'Freeze'}
                      </Button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Card>

      <Modal open={issueOpen} onClose={() => setIssueOpen(false)} title="Issue a new card">
        <form onSubmit={handleSubmit(onIssue)} className="flex flex-col gap-4">
          <Select label="Card type" {...register('type')}>
            <option value="Debit">Debit</option>
            <option value="Credit">Credit</option>
          </Select>
          <Input
            label="Spending limit"
            type="number"
            step="0.01"
            {...register('spendingLimit')}
            error={errors.spendingLimit?.message}
          />
          <Button type="submit" loading={issueCard.isPending}>
            Issue card
          </Button>
        </form>
      </Modal>
    </div>
  );
}
