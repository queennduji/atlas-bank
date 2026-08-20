import { forwardRef, type SelectHTMLAttributes } from 'react';
import { cn } from '@/lib/cn';

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, className, id, children, ...rest }, ref) => {
    const selectId = id ?? label?.toLowerCase().replace(/\s+/g, '-');
    return (
      <div className="flex flex-col gap-1.5">
        {label && (
          <label htmlFor={selectId} className="text-sm font-medium text-(--color-text)">
            {label}
          </label>
        )}
        <select
          id={selectId}
          ref={ref}
          className={cn(
            'h-10 rounded-lg border bg-(--color-surface) px-3 text-sm text-(--color-text) outline-none transition-colors',
            'focus:border-(--color-brand) focus:ring-2 focus:ring-(--color-brand-soft)',
            error ? 'border-(--color-negative)' : 'border-(--color-border)',
            className,
          )}
          {...rest}
        >
          {children}
        </select>
        {error && <p className="text-xs text-(--color-negative)">{error}</p>}
      </div>
    );
  },
);
Select.displayName = 'Select';
