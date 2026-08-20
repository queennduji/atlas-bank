import { Link, useParams } from 'react-router-dom';
import { ChevronLeft, Printer } from 'lucide-react';
import { useStatement } from '@/api/statements';
import { Card, CardBody } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { EmptyState } from '@/components/ui/EmptyState';
import { formatDate, formatMoney } from '@/lib/format';

export function StatementDetail() {
  const { id } = useParams<{ id: string }>();
  const { data: statement, isLoading } = useStatement(id);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (!statement) return <EmptyState title="Statement not found" />;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between print:hidden">
        <Link
          to="/statements"
          className="flex items-center gap-1 text-sm text-(--color-text-muted) hover:text-(--color-text)"
        >
          <ChevronLeft size={15} /> Back to statements
        </Link>
        <Button variant="secondary" size="sm" icon={<Printer size={14} />} onClick={() => window.print()}>
          Print
        </Button>
      </div>

      <Card>
        <CardBody className="flex flex-col gap-6">
          <div className="flex flex-wrap items-start justify-between gap-4 border-b border-(--color-border) pb-5">
            <div>
              <p className="text-lg font-semibold text-(--color-text)">Atlas Bank</p>
              <p className="text-sm text-(--color-text-muted)">Account statement</p>
            </div>
            <div className="text-right text-sm text-(--color-text-muted)">
              <p className="font-medium text-(--color-text)">{statement.customerName}</p>
              <p>{statement.accountNumber}</p>
              <p>
                {formatDate(statement.periodStart)} – {formatDate(statement.periodEnd)}
              </p>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {[
              ['Opening balance', statement.openingBalance],
              ['Closing balance', statement.closingBalance],
              ['Total credits', statement.totalCredits],
              ['Total debits', statement.totalDebits],
            ].map(([label, value]) => (
              <div key={label as string}>
                <p className="text-xs text-(--color-text-muted)">{label}</p>
                <p className="mt-1 text-base font-semibold tabular-nums text-(--color-text)">
                  {formatMoney(value as number, statement.currency)}
                </p>
              </div>
            ))}
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-(--color-border) text-left text-xs text-(--color-text-muted)">
                  <th className="py-2.5 pr-3 font-medium">Date</th>
                  <th className="py-2.5 pr-3 font-medium">Description</th>
                  <th className="py-2.5 pr-3 font-medium">Type</th>
                  <th className="py-2.5 pr-3 text-right font-medium">Amount</th>
                  <th className="py-2.5 pl-3 text-right font-medium">Balance</th>
                </tr>
              </thead>
              <tbody>
                {statement.lines.map((line) => (
                  <tr key={line.transactionId} className="border-b border-(--color-border) last:border-0">
                    <td className="py-2.5 pr-3 text-(--color-text-muted)">{formatDate(line.date)}</td>
                    <td className="py-2.5 pr-3 text-(--color-text)">{line.description || line.reference}</td>
                    <td className="py-2.5 pr-3 text-(--color-text-muted)">{line.type}</td>
                    <td className="py-2.5 pr-3 text-right tabular-nums text-(--color-text)">
                      {formatMoney(line.amount, statement.currency)}
                    </td>
                    <td className="py-2.5 pl-3 text-right tabular-nums text-(--color-text-muted)">
                      {formatMoney(line.runningBalance, statement.currency)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
