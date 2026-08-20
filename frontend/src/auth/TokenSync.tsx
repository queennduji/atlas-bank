import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { setAccessToken } from '@/api/authToken';

/** Keeps the plain-fetch API client's bearer token in sync with the OIDC session. */
export function TokenSync() {
  const auth = useAuth();

  useEffect(() => {
    setAccessToken(auth.user?.access_token ?? null);
  }, [auth.user?.access_token]);

  return null;
}
