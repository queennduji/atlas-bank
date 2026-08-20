import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import { ChevronDown, LogOut } from 'lucide-react';
import { useMe } from '@/api/customers';
import { initials } from '@/lib/format';

export function UserMenu() {
  const auth = useAuth();
  const { data: customer } = useMe(auth.isAuthenticated);
  const [open, setOpen] = useState(false);

  const displayName = customer ? `${customer.firstName} ${customer.lastName}` : auth.user?.profile.email;

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((o) => !o)}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
        className="flex items-center gap-2 rounded-lg py-1 pl-1 pr-2 hover:bg-(--color-surface-raised)"
      >
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-(--color-brand-soft) text-xs font-semibold text-(--color-brand)">
          {customer ? initials(customer.firstName, customer.lastName) : '··'}
        </div>
        <span className="hidden max-w-32 truncate text-sm font-medium text-(--color-text) sm:inline">
          {displayName ?? 'Account'}
        </span>
        <ChevronDown size={14} className="text-(--color-text-muted)" />
      </button>

      {open && (
        <div className="absolute right-0 top-11 z-20 w-44 overflow-hidden rounded-lg border border-(--color-border) bg-(--color-surface) shadow-(--shadow-card)">
          <button
            onClick={() => auth.signoutRedirect()}
            className="flex w-full items-center gap-2 px-3 py-2.5 text-left text-sm text-(--color-text) hover:bg-(--color-surface-raised)"
          >
            <LogOut size={15} />
            Sign out
          </button>
        </div>
      )}
    </div>
  );
}
