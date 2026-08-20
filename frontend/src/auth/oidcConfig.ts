import type { AuthProviderProps } from 'react-oidc-context';
import type { User } from 'oidc-client-ts';
import { WebStorageStateStore } from 'oidc-client-ts';

const KEYCLOAK_URL = import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080';
const KEYCLOAK_REALM = import.meta.env.VITE_KEYCLOAK_REALM ?? 'atlas-bank';
const KEYCLOAK_CLIENT_ID = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'atlas-bank-app';

/** Builds the OIDC config. `onSignedIn` receives the path the user was on before being sent to Keycloak. */
export function createOidcConfig(onSignedIn: (returnTo: string) => void): AuthProviderProps {
  return {
    authority: `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}`,
    client_id: KEYCLOAK_CLIENT_ID,
    redirect_uri: `${window.location.origin}/callback`,
    post_logout_redirect_uri: window.location.origin,
    response_type: 'code',
    scope: 'openid profile email',
    automaticSilentRenew: true,
    userStore: new WebStorageStateStore({ store: window.localStorage }),
    onSigninCallback: (user: User | void) => {
      window.history.replaceState({}, document.title, window.location.pathname);
      const state = user?.state as { returnTo?: string } | undefined;
      onSignedIn(state?.returnTo ?? '/dashboard');
    },
  };
}
