import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/Button';

export function NotFound() {
  return (
    <div className="flex h-screen flex-col items-center justify-center gap-3 bg-(--color-bg) text-center">
      <p className="text-sm font-medium text-(--color-brand)">404</p>
      <h1 className="text-xl font-semibold text-(--color-text)">Page not found</h1>
      <Link to="/">
        <Button variant="secondary" className="mt-2">
          Back home
        </Button>
      </Link>
    </div>
  );
}
