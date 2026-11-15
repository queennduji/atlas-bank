import { Link } from 'react-router-dom';
import { Eye } from 'lucide-react';
import { useIsDemo } from '@/auth/useIsDemo';

/** Strip across the top of the app while the shared, read-only demo account is signed in. */
export function DemoBanner() {
  const isDemo = useIsDemo();
  if (!isDemo) return null;

  return (
    <div
      role="status"
      className="flex shrink-0 items-center justify-center gap-2 bg-(--color-brand) px-4 py-2 text-center text-sm font-medium text-white"
    >
      <Eye size={15} className="shrink-0" />
      <span>
        You&apos;re in the read-only demo. Browse freely, but moving money is switched off.{' '}
        <Link to="/register" className="underline underline-offset-2 hover:opacity-90">
          Open your own account
        </Link>
      </span>
    </div>
  );
}
