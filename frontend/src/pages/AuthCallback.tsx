import { useAuth } from 'react-oidc-context';
import { Spinner } from '@/components/ui/Spinner';

/** Landing spot for Keycloak's redirect back after login. AuthRoot's onSigninCallback
 * does the actual navigation once the token exchange finishes; this just holds the screen. */
export function AuthCallback() {
  const auth = useAuth();

  return (
    <div className="flex h-screen flex-col items-center justify-center gap-3 bg-(--color-bg)">
      <Spinner />
      <p className="text-sm text-(--color-text-muted)">
        {auth.error ? `Sign-in failed: ${auth.error.message}` : 'Signing you in…'}
      </p>
    </div>
  );
}
