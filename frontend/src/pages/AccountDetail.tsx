import { Link, useParams } from 'react-router-dom';
import { ArrowDownLeft, ArrowUpRight, ArrowLeftRight, FileText, ChevronLeft } from 'lucide-react';
import { useAccount } from '@/api/accounts';
import { useAccountTransactions } from '@/api/transactions';
import { AccountTypeLabel, TransactionTypeLabel } from '@/api/types';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { EmptyState } from '@/components/ui/EmptyState';
import { formatMoney, formatDateTime } from '@/lib/format';
import { accountStatusBadge, transactionStatusBadge } from '@/lib/badges';
import { DEMO_READ_ONLY_HINT, useIsDemo } from '@/auth/useIsDemo';

const moneyActions = [
  { mode: 'deposit', label: 'Deposit', icon: <ArrowDownLeft size={14} /> },
  { mode: 'withdraw', label: 'Withdraw', icon: <ArrowUpRight size={14} /> },
  { mode: 'transfer', label: 'Transfer', icon: <ArrowLeftRight size={14} /> },
];

export function AccountDetail() {
  const { id } = useParams<{ id: string }>();
  const isDemo = useIsDemo();
  const { data: account, isLoading } = useAccount(id);
  const { data: transactions, isLoading: txLoading } = useAccountTransactions(id);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (!account) {
    return <EmptyState title="Account not found" />;
  }

  const status = accountStatusBadge(account.status);

  return (
    <div className="flex flex-col gap-6">
      <Link to="/dashboard" className="flex w-fit items-center gap-1 text-sm text-(--color-text-muted) hover:text-(--color-text)">
        <ChevronLeft size={15} /> Back to dashboard
      </Link>

      <Card>
        <CardBody className="flex flex-col gap-4">
          <div className="flex items-start justify-between">
            <div>
              <p className="text-sm font-medium text-(--color-text-muted)">
                {AccountTypeLabel[account.type]} · {account.accountNumber}
              </p>
              <p className="mt-1 text-3xl font-semibold tabular-nums text-(--color-text)">
                {formatMoney(account.balance, account.currency)}
              </p>
            </div>
            <Badge tone={status.tone}>{status.label}</Badge>
          </div>

          <div className="flex flex-wrap gap-2 border-t border-(--color-border) pt-4">
            {moneyActions.map(({ mode, label, icon }) =>
              isDemo ? (
                // A disabled button inside a Link can still navigate in some browsers, so
                // in demo mode these render as plain disabled buttons.
                <Button key={mode} variant="secondary" size="sm" icon={icon} disabled title={DEMO_READ_ONLY_HINT}>
                  {label}
                </Button>
              ) : (
                <Link key={mode} to={`/transfer?accountId=${account.id}&mode=${mode}`}>
                  <Button variant="secondary" size="sm" icon={icon}>
                    {label}
                  </Button>
                </Link>
              ),
            )}
            <Link to={`/statements?accountId=${account.id}`}>
              <Button variant="secondary" size="sm" icon={<FileText size={14} />}>
                Statements
              </Button>
            </Link>
          </div>
        </CardBody>
      </Card>

      <Card>
        <CardHeader title="Transaction history" />
        {txLoading ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : !transactions || transactions.length === 0 ? (
          <EmptyState title="No transactions yet" description="Deposits, withdrawals, and transfers will show up here." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-(--color-border) text-left text-xs text-(--color-text-muted)">
                  <th className="px-5 py-2.5 font-medium">Date</th>
                  <th className="px-5 py-2.5 font-medium">Type</th>
                  <th className="px-5 py-2.5 font-medium">Reference</th>
                  <th className="px-5 py-2.5 font-medium">Status</th>
                  <th className="px-5 py-2.5 text-right font-medium">Amount</th>
                </tr>
              </thead>
              <tbody>
                {[...transactions]
                  .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
                  .map((tx) => {
                    const txStatus = transactionStatusBadge(tx.status);
                    const outgoing = tx.type === 1 || (tx.type === 2 && tx.accountId === account.id);
                    return (
                      <tr key={tx.id} className="border-b border-(--color-border) last:border-0">
                        <td className="px-5 py-3 text-(--color-text-muted)">{formatDateTime(tx.createdAt)}</td>
                        <td className="px-5 py-3 text-(--color-text)">{TransactionTypeLabel[tx.type]}</td>
                        <td className="px-5 py-3 font-mono-tabular text-xs text-(--color-text-faint)">
                          {tx.reference}
                        </td>
                        <td className="px-5 py-3">
                          <Badge tone={txStatus.tone}>{txStatus.label}</Badge>
                        </td>
                        <td
                          className={
                            'px-5 py-3 text-right tabular-nums font-medium ' +
                            (outgoing ? 'text-(--color-text)' : 'text-(--color-positive)')
                          }
                        >
                          {outgoing ? '-' : '+'}
                          {formatMoney(tx.amount, tx.currency)}
                        </td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
