import { useEffect } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { Spinner } from '@/components/ui/Spinner';

export function ProtectedRoute() {
  const auth = useAuth();
  const location = useLocation();

  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated && !auth.activeNavigator) {
      auth.signinRedirect({ state: { returnTo: location.pathname + location.search } });
    }
  }, [auth, location.pathname, location.search]);

  if (!auth.isAuthenticated) {
    return (
      <div className="flex h-screen items-center justify-center bg-(--color-bg)">
        <Spinner />
      </div>
    );
  }

  return <Outlet />;
}
