// Small indirection so the plain-fetch API client (api/client.ts) can read the
// current OIDC access token without importing React/context machinery. The
// auth layer calls setAccessToken() whenever the signed-in user changes.

let currentToken: string | null = null;

export function setAccessToken(token: string | null): void {
  currentToken = token;
}

export function getAccessToken(): string | null {
  return currentToken;
}
