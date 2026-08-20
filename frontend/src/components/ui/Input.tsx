import { forwardRef, type InputHTMLAttributes } from 'react';
import { cn } from '@/lib/cn';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  hint?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, hint, className, id, type, onWheel, ...rest }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-');
    return (
      <div className="flex flex-col gap-1.5">
        {label && (
          <label htmlFor={inputId} className="text-sm font-medium text-(--color-text)">
            {label}
          </label>
        )}
        <input
          id={inputId}
          ref={ref}
          type={type}
          onWheel={(e) => {
            // Browsers silently change a focused number input's value when the
            // page is scrolled (by `step`, here 0.01) — a user scrolling right
            // after typing an amount, with the cursor still over the field, can
            // submit a different figure than they typed without ever noticing.
            // Blurring drops focus so the scroll just scrolls the page instead.
            if (type === 'number') e.currentTarget.blur();
            onWheel?.(e);
          }}
          className={cn(
            'h-10 rounded-lg border bg-(--color-surface) px-3 text-sm text-(--color-text) outline-none transition-colors placeholder:text-(--color-text-faint)',
            'focus:border-(--color-brand) focus:ring-2 focus:ring-(--color-brand-soft)',
            error ? 'border-(--color-negative)' : 'border-(--color-border)',
            className,
          )}
          {...rest}
        />
        {error ? (
          <p className="text-xs text-(--color-negative)">{error}</p>
        ) : hint ? (
          <p className="text-xs text-(--color-text-muted)">{hint}</p>
        ) : null}
      </div>
    );
  },
);
Input.displayName = 'Input';
