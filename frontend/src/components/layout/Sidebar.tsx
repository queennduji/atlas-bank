import { Link, NavLink } from 'react-router-dom';
import { LayoutDashboard, ArrowLeftRight, CreditCard, FileText, User, Landmark } from 'lucide-react';
import { cn } from '@/lib/cn';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/transfer', label: 'Move Money', icon: ArrowLeftRight },
  { to: '/cards', label: 'Cards', icon: CreditCard },
  { to: '/statements', label: 'Statements', icon: FileText },
  { to: '/profile', label: 'Profile', icon: User },
];

export function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <aside className="flex h-full w-60 shrink-0 flex-col border-r border-(--color-border) bg-(--color-surface)">
      <Link to="/dashboard" onClick={onNavigate} className="flex h-16 items-center gap-2 px-5">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-(--color-brand) text-white">
          <Landmark size={16} />
        </div>
        <span className="text-sm font-semibold tracking-tight text-(--color-text)">Atlas Bank</span>
      </Link>

      <nav className="flex flex-1 flex-col gap-1 px-3 py-2">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-(--color-brand-soft) text-(--color-brand)'
                  : 'text-(--color-text-muted) hover:bg-(--color-surface-raised) hover:text-(--color-text)',
              )
            }
          >
            <Icon size={17} />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="border-t border-(--color-border) px-5 py-4 text-xs text-(--color-text-faint)">
        Portfolio demo - not a real bank.
      </div>
    </aside>
  );
}
