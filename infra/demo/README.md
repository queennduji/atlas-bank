# Read-only demo account

A shared login for recruiters and reviewers who want to click around a populated
account without registering. The landing page has a **Try the read-only demo** button;
the Keycloak login page has a matching **Sign in as demo** panel.

## How read-only is enforced

The guarantee lives in one place: `DemoReadOnlyMiddleware` in the API gateway. A request
from the demo user is allowed if it is a GET/HEAD/OPTIONS and answered `403` otherwise,
before it reaches a service. The demo user is recognised two ways, and either is enough:
the `demo-viewer` realm role, or the account's own username/email (`Demo:Email` in the
gateway's `appsettings.json`). The second is a backstop so the check fails closed: if a
Keycloak change ever kept the role out of the token, the public-password account would
otherwise quietly gain write access. Everything in the frontend (the banner, the disabled
buttons) is courtesy UX on top of that and can be ignored by a determined visitor without
consequence.

Keycloak's own self-service pages are a separate door the gateway can't see: any signed-in
user can open "change password", "edit profile" or "set up an authenticator" from a login
URL carrying `kc_action=...`, which would let a visitor take over the shared account. Nothing
in this project sends that parameter, so nginx refuses it on `auth.atlasbank.dev`
(including inside a `request` object, and POSTs to the authorization endpoint). Check
`nginx/nginx.conf` before changing those rules.

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
4. locks the account down again: only `demo-viewer`, the password as the only credential
   (any authenticator or security key is removed), name and email back to their known
   values, existing sessions ended, brute-force lockout cleared;
5. proves the lock-down by reading with a fresh token (expects 200) and attempting a
   write (expects 403), and exits non-zero if either is wrong.

All data is synthetic.

**In production, run it on the VM:**

```bash
sh ~/atlas-bank/infra/demo/seed-on-vm.sh
```

`seed-on-vm.sh` runs the seeder in a throwaway container on the compose network, talking to
Keycloak and the gateway directly (`http://keycloak:8080`, `http://api-gateway:8080`). It
has to work this way because nginx refuses Keycloak's admin API and the `master` realm from
the internet (see below), so the old approach of running the script from a laptop against
`https://auth.atlasbank.dev` gets a 403. The script reads `KEYCLOAK_ADMIN_PASSWORD` from the
repo's `.env`, hands it to the container by variable name, and never prints it.

**Locally**, with the default `docker compose up` (no nginx in front), run the script
directly; no variables beyond the admin credentials are needed:

```bash
KC_ADMIN_USER=admin KC_ADMIN_PASSWORD=admin python3 infra/demo/seed_demo.py
```

## Keycloak admin access

`auth.atlasbank.dev` serves only what the public needs. nginx returns 403 for `/admin`
(the console and the admin REST API) and `/realms/master` (where the admin account signs
in, including the password-grant token endpoint), plus any path containing a `;` (Keycloak
treats `;param` as a path parameter wherever it appears, so `/realms;x=1/master/...` would
otherwise get around a prefix rule). The `atlas-bank` realm's brute-force protection doesn't
cover the admin account, so leaving these open let the admin password be guessed at full speed.

Nothing in the project is affected: the services call Keycloak's admin API over the Docker
network, which doesn't pass through nginx. For your own admin work, use the CLI inside the
container (`docker exec atlas-bank-keycloak-1 /opt/keycloak/bin/kcadm.sh ...`), or to get the
browser console back for a while, comment out the `location ~* "(^/(admin|realms/master)|;)"`
block in `nginx/nginx.conf` and recreate nginx; put it back afterwards.

## Before the first run

Deploy the gateway build that contains the middleware, so the seeder's last check can
pass. The `demo-viewer` realm role is created by the seeder if the realm doesn't have it
yet (a fresh import from `keycloak/realm-export.json` already does).

## Known limits

- Anyone can type the demo username with a wrong password five times and trigger the
  realm's brute-force lockout, which temporarily blocks the demo login. Re-running the
  seeder clears it immediately.
- The shared account can be tampered with only through a hole this setup is meant to have
  closed (the `kc_action` block in nginx). If that rule is ever removed, or Keycloak grows
  another self-service route, re-running the seeder puts the account back as described
  above, but the lasting fix is closing the route.
- The "forgot password" link is visible to the demo user, but the realm has no SMTP
  configured, so it can't send anything.
- Per-IP rate limiting at the gateway (100 requests/minute) applies to demo traffic like
  any other.
