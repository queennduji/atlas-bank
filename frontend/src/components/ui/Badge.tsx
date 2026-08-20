import type { ReactNode } from 'react';
import { cn } from '@/lib/cn';

type Tone = 'positive' | 'negative' | 'warning' | 'neutral' | 'brand';

const toneClasses: Record<Tone, string> = {
  positive: 'bg-(--color-positive-soft) text-(--color-positive)',
  negative: 'bg-(--color-negative-soft) text-(--color-negative)',
  warning: 'bg-(--color-warning-soft) text-(--color-warning)',
  neutral: 'bg-(--color-surface-raised) text-(--color-text-muted) border border-(--color-border)',
  brand: 'bg-(--color-brand-soft) text-(--color-brand)',
};

export function Badge({ tone = 'neutral', children }: { tone?: Tone; children: ReactNode }) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        toneClasses[tone],
      )}
    >
      {children}
    </span>
  );
}
