import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link } from 'react-router-dom';
import { Plus, Wallet, PiggyBank, ArrowRight } from 'lucide-react';
import { useMe } from '@/api/customers';
import { useCreateAccount, useMyAccounts } from '@/api/accounts';
import { AccountType, AccountTypeLabel, type AccountTypeValue } from '@/api/types';
import { ApiError } from '@/api/client';
import { Card, CardBody } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';
import { Select } from '@/components/ui/Select';
import { EmptyState } from '@/components/ui/EmptyState';
import { Spinner } from '@/components/ui/Spinner';
import { useToast } from '@/components/ui/Toast';
import { formatMoney } from '@/lib/format';
import { accountStatusBadge } from '@/lib/badges';

const currencies = ['USD', 'EUR', 'GBP'];

const newAccountSchema = z.object({
  type: z.string().min(1),
  currency: z.string().min(1),
});
type NewAccountForm = z.infer<typeof newAccountSchema>;

export function Dashboard() {
  const { data: customer, isError: customerError, error } = useMe(true);
  const { data: accounts, isLoading: accountsLoading } = useMyAccounts(true);
  const createAccount = useCreateAccount();
  const toast = useToast();
  const [modalOpen, setModalOpen] = useState(false);

  const { register, handleSubmit, reset } = useForm<NewAccountForm>({
    resolver: zodResolver(newAccountSchema),
    defaultValues: { type: String(AccountType.Checking), currency: 'USD' },
  });

  async function onSubmit(values: NewAccountForm) {
    try {
      await createAccount.mutateAsync({
        type: Number(values.type) as AccountTypeValue,
        currency: values.currency,
      });
      toast.success('Account opened.');
      setModalOpen(false);
      reset();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Could not open account.');
    }
  }

  if (customerError && error instanceof ApiError && error.status === 404) {
    return (
      <EmptyState
        title="No customer profile found"
        description="Your sign-in doesn't have a linked Atlas Bank profile yet. Register to continue."
        action={
          <Link to="/register">
            <Button>Complete registration</Button>
          </Link>
        }
      />
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-(--color-text)">
            {customer ? `Welcome back, ${customer.firstName}` : 'Welcome back'}
          </h1>
          <p className="mt-0.5 text-sm text-(--color-text-muted)">Here's what's happening across your accounts.</p>
        </div>
        <Button icon={<Plus size={16} />} onClick={() => setModalOpen(true)}>
          New account
        </Button>
      </div>

      {accountsLoading ? (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      ) : !accounts || accounts.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Wallet size={28} />}
            title="No accounts yet"
            description="Open your first checking or savings account to get started."
            action={<Button onClick={() => setModalOpen(true)}>Open an account</Button>}
          />
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {accounts.map((account) => {
            const status = accountStatusBadge(account.status);
            return (
              <Link key={account.id} to={`/accounts/${account.id}`}>
                <Card className="h-full transition-shadow hover:shadow-md">
                  <CardBody className="flex flex-col gap-4">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2 text-(--color-text-muted)">
                        {account.type === AccountType.Savings ? <PiggyBank size={16} /> : <Wallet size={16} />}
                        <span className="text-sm font-medium">{AccountTypeLabel[account.type]}</span>
                      </div>
                      <Badge tone={status.tone}>{status.label}</Badge>
                    </div>
                    <div>
                      <p className="text-2xl font-semibold tabular-nums text-(--color-text)">
                        {formatMoney(account.balance, account.currency)}
                      </p>
                      <p className="mt-1 font-mono-tabular text-xs text-(--color-text-faint)">
                        {account.accountNumber}
                      </p>
                    </div>
                    <div className="flex items-center gap-1 text-xs font-medium text-(--color-brand)">
                      View account <ArrowRight size={13} />
                    </div>
                  </CardBody>
                </Card>
              </Link>
            );
          })}
        </div>
      )}

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Open a new account">
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <Select label="Account type" {...register('type')}>
            <option value={AccountType.Checking}>Checking</option>
            <option value={AccountType.Savings}>Savings</option>
          </Select>
          <Select label="Currency" {...register('currency')}>
            {currencies.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </Select>
          <Button type="submit" loading={createAccount.isPending}>
            Open account
          </Button>
        </form>
      </Modal>
    </div>
  );
}
