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

Keycloak's own self-service pages sit outside the gateway, so they are closed off
separately: nothing in this project uses the `kc_action` login parameter, so nginx refuses
it on `auth.atlasbank.dev`, along with `request` objects and POSTs to the authorization
endpoint. See `nginx/nginx.conf` before changing those rules. The seeder also leaves the demo
user with `demo-viewer` and no other roles.

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
(the console and the admin REST API), `/realms/master` (the admin realm), and any path
containing a `;`. Admin tooling runs on the VM instead.

Nothing in the project is affected: the services call Keycloak's admin API over the Docker
network, which doesn't pass through nginx. For your own admin work, use the CLI inside the
container (`docker exec atlas-bank-keycloak-1 /opt/keycloak/bin/kcadm.sh ...`), run a helper
script with `sh ~/atlas-bank/infra/run-in-network.sh <script>`, or to get the browser console
back for a while, comment out the `location ~* "(^/(admin|realms/master)|;)"`
block in `nginx/nginx.conf` and recreate nginx; put it back afterwards.

## Before the first run

Deploy the gateway build that contains the middleware, so the seeder's last check can
pass. The `demo-viewer` realm role is created by the seeder if the realm doesn't have it
yet (a fresh import from `keycloak/realm-export.json` already does).

## Known limits

- If the demo login ever stops working or the account looks changed, re-run the seeder: it
  puts the account back exactly as described above, including clearing any login lockout.
- Per-IP rate limiting at the gateway (100 requests/minute) applies to demo traffic like
  any other.
