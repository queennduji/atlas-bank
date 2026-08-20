import { type ReactNode, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthProvider } from 'react-oidc-context';
import { createOidcConfig } from './oidcConfig';

/** Wraps the app in react-oidc-context's AuthProvider, wired to React Router so a
 * post-login redirect lands back on the page the user was trying to reach. */
export function AuthRoot({ children }: { children: ReactNode }) {
  const navigate = useNavigate();

  const handleSignedIn = useCallback(
    (returnTo: string) => navigate(returnTo, { replace: true }),
    [navigate],
  );

  const config = createOidcConfig(handleSignedIn);

  return <AuthProvider {...config}>{children}</AuthProvider>;
}
