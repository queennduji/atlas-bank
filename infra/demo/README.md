# Read-only demo account

A shared login for recruiters and reviewers who want to click around a populated
account without registering. The landing page has a **Try the read-only demo** button;
the Keycloak login page has a matching **Sign in as demo** panel.

## How read-only is enforced

The guarantee lives in one place: `DemoReadOnlyMiddleware` in the API gateway. Any
request carrying a token with the `demo-viewer` realm role is allowed if it is a
GET/HEAD/OPTIONS and answered `403` otherwise, before it reaches a service. Everything
in the frontend (the banner, the disabled buttons) is courtesy UX on top of that and
can be ignored by a determined visitor without consequence.

The seeder also takes every other role away. Stock Keycloak gives users `manage-account`,
which would let the demo user change the shared password from the account console, so the
demo user is left with `demo-viewer` and nothing else (checked against a local Keycloak: no
realm roles besides `demo-viewer`, no account-client roles).

The login is public by design (the Keycloak page prints it), so the password is not a
secret. It lives in `keycloak/themes/atlas-bank/login/theme.properties` and in
`seed_demo.py`; keep the two in sync.

## Setting it up / resetting it

`seed_demo.py` is idempotent and is also the reset button. Each run:

1. creates the demo customer through the public registration API if it doesn't exist
   (so the Keycloak user and the Customer record are made by the real code path);
2. takes `demo-viewer` off for the duration, because it would block the seeding writes;
3. opens a checking and a savings account and posts sample deposits, withdrawals and
   transfers, issues a debit and a (frozen) credit card, and generates a statement –
   but only if the account has no data yet, so re-runs never pile up duplicates;
4. locks the account down again (only `demo-viewer`, password reset to the known
   value, brute-force lockout cleared);
5. proves the lock-down by reading with a fresh token (expects 200) and attempting a
   write (expects 403), and exits non-zero if either is wrong.

All data is synthetic. Run it wherever it can reach Keycloak and the gateway, passing
the Keycloak admin credentials in the environment (never as arguments, never committed):

```bash
KEYCLOAK_URL=https://auth.atlasbank.dev \
GATEWAY_URL=https://api.atlasbank.dev \
KC_ADMIN_USER=<admin user> KC_ADMIN_PASSWORD=<admin password> \
python3 infra/demo/seed_demo.py
```

Locally, with the default `docker compose up`, no variables beyond the admin
credentials are needed.

## Before the first run

1. Create the realm role once: in the Keycloak admin console, Realm roles, Create role,
   named `demo-viewer`. (A fresh import from `keycloak/realm-export.json` already has
   it; the live realm does not.)

2. Deploy the gateway build that contains the middleware, so step 5 above can pass.

## Known limits

- Anyone can type the demo username with a wrong password five times and trigger the
  realm's brute-force lockout, which temporarily blocks the demo login. Re-running the
  seeder clears it immediately.
- Not tested: Keycloak's application-initiated actions (an authorize URL carrying
  `kc_action=UPDATE_PASSWORD`) might let a signed-in demo user change the shared password
  without any account-console role. If someone does, re-running the seeder restores it.
- The "forgot password" link is visible to the demo user, but the realm has no SMTP
  configured, so it can't send anything.
- Per-IP rate limiting at the gateway (100 requests/minute) applies to demo traffic like
  any other.
