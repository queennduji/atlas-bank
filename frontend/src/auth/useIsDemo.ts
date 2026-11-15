import { useMemo } from 'react';
import { useAuth } from 'react-oidc-context';

const DEMO_ROLE = 'demo-viewer';

/** Username the "Try the demo" button hands to Keycloak as login_hint. */
export const DEMO_USERNAME = import.meta.env.VITE_DEMO_USERNAME ?? 'demo@atlasbank.dev';

/** Tooltip for controls that are switched off in demo mode. */
export const DEMO_READ_ONLY_HINT = 'Read-only demo account';

function realmRoles(accessToken: string | undefined): string[] {
  if (!accessToken) return [];
  try {
    const payload = accessToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const claims = JSON.parse(atob(payload));
    return Array.isArray(claims.realm_access?.roles) ? claims.realm_access.roles : [];
  } catch {
    return [];
  }
}

/**
 * True when the signed-in user is the shared demo account. This only drives the UI
 * (banner, disabled buttons); the actual guarantee is the API gateway refusing every
 * non-GET request for tokens carrying this role, so nothing here is security-relevant.
 */
export function useIsDemo(): boolean {
  const auth = useAuth();
  const accessToken = auth.user?.access_token;
  return useMemo(() => realmRoles(accessToken).includes(DEMO_ROLE), [accessToken]);
}
