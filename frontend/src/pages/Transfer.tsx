import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowDownLeft, ArrowUpRight, ArrowLeftRight } from 'lucide-react';
import { useMyAccounts } from '@/api/accounts';
import { useDeposit, useTransfer, useWithdraw } from '@/api/transactions';
import { AccountTypeLabel } from '@/api/types';
import { Card, CardBody } from '@/components/ui/Card';
import { Select } from '@/components/ui/Select';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { useToast } from '@/components/ui/Toast';
import { ApiError } from '@/api/client';
import { formatMoney } from '@/lib/format';
import { cn } from '@/lib/cn';

type Mode = 'deposit' | 'withdraw' | 'transfer';

const tabs: { key: Mode; label: string; icon: typeof ArrowDownLeft }[] = [
  { key: 'deposit', label: 'Deposit', icon: ArrowDownLeft },
  { key: 'withdraw', label: 'Withdraw', icon: ArrowUpRight },
  { key: 'transfer', label: 'Transfer', icon: ArrowLeftRight },
];

const schema = z.object({
  accountId: z.string().min(1, 'Select an account'),
  toAccountId: z.string().optional(),
  amount: z.string().refine((v) => Number(v) > 0, 'Enter an amount greater than 0'),
  description: z.string().optional(),
});
type FormValues = z.infer<typeof schema>;

export function Transfer() {
  const [params] = useSearchParams();
  const { data: accounts, isLoading } = useMyAccounts(true);
  const [mode, setMode] = useState<Mode>((params.get('mode') as Mode) ?? 'deposit');
  const toast = useToast();

  const deposit = useDeposit();
  const withdraw = useWithdraw();
  const transfer = useTransfer();
  const pending = deposit.isPending || withdraw.isPending || transfer.isPending;

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { accountId: params.get('accountId') ?? '' },
  });

  const selectedAccount = useMemo(
    () => accounts?.find((a) => a.id === watch('accountId')),
    [accounts, watch('accountId')],
  );

  async function onSubmit(values: FormValues) {
    const amount = Number(values.amount);
    try {
      if (mode === 'deposit') {
        await deposit.mutateAsync({ accountId: values.accountId, amount, description: values.description });
        toast.success(`Deposited ${formatMoney(amount, selectedAccount?.currency ?? 'USD')}.`);
      } else if (mode === 'withdraw') {
        await withdraw.mutateAsync({ accountId: values.accountId, amount, description: values.description });
        toast.success(`Withdrew ${formatMoney(amount, selectedAccount?.currency ?? 'USD')}.`);
      } else {
        if (!values.toAccountId) throw new Error('Enter a destination account ID.');
        await transfer.mutateAsync({
          fromAccountId: values.accountId,
          toAccountId: values.toAccountId,
          amount,
          description: values.description,
        });
        toast.success(`Transferred ${formatMoney(amount, selectedAccount?.currency ?? 'USD')}.`);
      }
      reset({ accountId: values.accountId, toAccountId: '', amount: '', description: '' });
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : err instanceof Error ? err.message : 'Something went wrong.');
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-xl font-semibold text-(--color-text)">Move money</h1>
        <p className="mt-0.5 text-sm text-(--color-text-muted)">Deposit, withdraw, or transfer between accounts.</p>
      </div>

      <div className="flex gap-1 rounded-lg border border-(--color-border) bg-(--color-surface) p-1 w-fit">
        {tabs.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setMode(key)}
            className={cn(
              'flex items-center gap-1.5 rounded-md px-3.5 py-1.5 text-sm font-medium transition-colors',
              mode === key
                ? 'bg-(--color-brand-soft) text-(--color-brand)'
                : 'text-(--color-text-muted) hover:text-(--color-text)',
            )}
          >
            <Icon size={14} />
            {label}
          </button>
        ))}
      </div>

      <Card className="max-w-md">
        <CardBody>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <Select
              label={mode === 'transfer' ? 'From account' : 'Account'}
              disabled={isLoading}
              {...register('accountId')}
              error={errors.accountId?.message}
            >
              <option value="">Select an account…</option>
              {accounts?.map((a) => (
                <option key={a.id} value={a.id}>
                  {AccountTypeLabel[a.type]} · {a.accountNumber} · {formatMoney(a.balance, a.currency)}
                </option>
              ))}
            </Select>

            {mode === 'transfer' && (
              <Input
                label="To account ID"
                placeholder="Destination account GUID"
                hint="Ask the recipient for their account ID, or pick one of your own below."
                {...register('toAccountId')}
              />
            )}

            {mode === 'transfer' && accounts && accounts.length > 1 && (
              <div className="flex flex-wrap gap-1.5">
                {accounts
                  .filter((a) => a.id !== watch('accountId'))
                  .map((a) => (
                    <button
                      key={a.id}
                      type="button"
                      onClick={() => setValue('toAccountId', a.id, { shouldValidate: true })}
                      className="rounded-full border border-(--color-border) px-2.5 py-1 text-xs text-(--color-text-muted) hover:border-(--color-brand) hover:text-(--color-brand)"
                    >
                      My {AccountTypeLabel[a.type]} · {a.accountNumber}
                    </button>
                  ))}
              </div>
            )}

            <Input
              label="Amount"
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
              {...register('amount')}
              error={errors.amount?.message}
            />
            <Input label="Description (optional)" {...register('description')} />

            <Button type="submit" loading={pending}>
              Confirm {mode}
            </Button>
          </form>
        </CardBody>
      </Card>
    </div>
  );
}
