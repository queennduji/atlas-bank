import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FileText, Plus } from 'lucide-react';
import { useMyAccounts } from '@/api/accounts';
import { useAccountStatements, useGenerateStatement } from '@/api/statements';
import { AccountTypeLabel } from '@/api/types';
import { Card } from '@/components/ui/Card';
import { Select } from '@/components/ui/Select';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { Spinner } from '@/components/ui/Spinner';
import { EmptyState } from '@/components/ui/EmptyState';
import { useToast } from '@/components/ui/Toast';
import { ApiError } from '@/api/client';
import { formatDate, formatMoney } from '@/lib/format';

function defaultPeriod() {
  const end = new Date();
  const start = new Date();
  start.setMonth(start.getMonth() - 1);
  return { start: start.toISOString().slice(0, 10), end: end.toISOString().slice(0, 10) };
}

export function Statements() {
  const [params] = useSearchParams();
  const { data: accounts } = useMyAccounts(true);
  const [accountId, setAccountId] = useState(params.get('accountId') ?? '');
  const activeAccountId = accountId || accounts?.[0]?.id;

  const { data: statements, isLoading } = useAccountStatements(activeAccountId);
  const generate = useGenerateStatement();
  const toast = useToast();

  const [modalOpen, setModalOpen] = useState(false);
  const [period, setPeriod] = useState(defaultPeriod());

  async function onGenerate() {
    if (!activeAccountId) return;
    try {
      await generate.mutateAsync({
        accountId: activeAccountId,
        periodStart: new Date(period.start).toISOString(),
        periodEnd: new Date(period.end).toISOString(),
      });
      toast.success('Statement generated.');
      setModalOpen(false);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Could not generate statement.');
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-(--color-text)">Statements</h1>
          <p className="mt-0.5 text-sm text-(--color-text-muted)">Generate and review account statements.</p>
        </div>
        <div className="flex items-center gap-2">
          <Select value={accountId || activeAccountId || ''} onChange={(e) => setAccountId(e.target.value)}>
            {accounts?.map((a) => (
              <option key={a.id} value={a.id}>
                {AccountTypeLabel[a.type]} · {a.accountNumber}
              </option>
            ))}
          </Select>
          <Button icon={<Plus size={16} />} onClick={() => setModalOpen(true)} disabled={!activeAccountId}>
            Generate
          </Button>
        </div>
      </div>

      <Card>
        {isLoading ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : !statements || statements.length === 0 ? (
          <EmptyState
            icon={<FileText size={28} />}
            title="No statements yet"
            description="Generate a statement for a date range to see it here."
            action={
              <Button onClick={() => setModalOpen(true)} disabled={!activeAccountId}>
                Generate statement
              </Button>
            }
          />
        ) : (
          <div className="divide-y divide-(--color-border)">
            {statements.map((s) => (
              <Link
                key={s.id}
                to={`/statements/${s.id}`}
                className="flex items-center justify-between gap-3 px-5 py-4 hover:bg-(--color-surface-raised)"
              >
                <div>
                  <p className="text-sm font-medium text-(--color-text)">
                    {formatDate(s.periodStart)} – {formatDate(s.periodEnd)}
                  </p>
                  <p className="text-xs text-(--color-text-muted)">Generated {formatDate(s.generatedAt)}</p>
                </div>
                <p className="text-sm font-medium tabular-nums text-(--color-text)">
                  {formatMoney(s.closingBalance)}
                </p>
              </Link>
            ))}
          </div>
        )}
      </Card>

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Generate statement">
        <div className="flex flex-col gap-4">
          <Input
            label="Period start"
            type="date"
            value={period.start}
            onChange={(e) => setPeriod((p) => ({ ...p, start: e.target.value }))}
          />
          <Input
            label="Period end"
            type="date"
            value={period.end}
            onChange={(e) => setPeriod((p) => ({ ...p, end: e.target.value }))}
          />
          <Button onClick={onGenerate} loading={generate.isPending}>
            Generate
          </Button>
        </div>
      </Modal>
    </div>
  );
}
