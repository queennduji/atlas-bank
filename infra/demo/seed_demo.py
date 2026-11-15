#!/usr/bin/env python3
"""Creates (or resets) the read-only demo account and fills it with sample data.

Safe to re-run: it reuses the user if it exists, only seeds banking data when the
account has none yet, and always finishes by re-locking the account down (known
password, `demo-viewer` role only, brute-force lockout cleared). If someone ever
vandalises the shared demo login, re-running this puts it back.

Everything the data needs goes through the public API as the demo user itself, so
the accounts, ledger entries, cards and statements are produced by the real
services rather than inserted into their databases. All of it is synthetic.

Required environment:
  KC_ADMIN_USER, KC_ADMIN_PASSWORD   Keycloak master-realm admin (never committed)
Optional environment:
  KEYCLOAK_URL   default http://localhost:8080      (prod: https://auth.atlasbank.dev)
  GATEWAY_URL    default http://localhost:5000      (prod: https://api.atlasbank.dev)
  KEYCLOAK_REALM default atlas-bank

Only the Python standard library is used.
"""
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from datetime import datetime, timezone

KEYCLOAK_URL = os.environ.get("KEYCLOAK_URL", "http://localhost:8080").rstrip("/")
GATEWAY_URL = os.environ.get("GATEWAY_URL", "http://localhost:5000").rstrip("/")
REALM = os.environ.get("KEYCLOAK_REALM", "atlas-bank")
CLIENT_ID = "atlas-bank-app"

# The demo login is public on purpose: the Keycloak login page shows it, so there is
# nothing to protect in the password itself. What protects the system is that the
# account can only read. Keep these two in sync with
# keycloak/themes/atlas-bank/login/theme.properties.
DEMO_EMAIL = "demo@atlasbank.dev"
DEMO_PASSWORD = "Demo-Viewer-2026"
DEMO_ROLE = "demo-viewer"


def call(method, url, *, token=None, body=None, form=None, headers=None, expect=(200, 201, 204)):
    """Small JSON-over-HTTP helper. Returns (status, parsed_body, response_headers)."""
    data = None
    req_headers = {"Accept": "application/json", **(headers or {})}
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        req_headers["Content-Type"] = "application/x-www-form-urlencoded"
    elif body is not None:
        data = json.dumps(body).encode()
        req_headers["Content-Type"] = "application/json"
    if token:
        req_headers["Authorization"] = f"Bearer {token}"

    req = urllib.request.Request(url, data=data, method=method, headers=req_headers)
    try:
        with urllib.request.urlopen(req, timeout=30) as res:
            status, raw, res_headers = res.status, res.read(), res.headers
    except urllib.error.HTTPError as err:
        status, raw, res_headers = err.code, err.read(), err.headers

    text = raw.decode(errors="replace")
    try:
        parsed = json.loads(text) if text else None
    except json.JSONDecodeError:
        parsed = text

    if expect is not None and status not in expect:
        sys.exit(f"{method} {url} -> HTTP {status}: {text[:300]}")
    return status, parsed, res_headers


def admin_token():
    user, password = os.environ.get("KC_ADMIN_USER"), os.environ.get("KC_ADMIN_PASSWORD")
    if not user or not password:
        sys.exit("Set KC_ADMIN_USER and KC_ADMIN_PASSWORD in the environment first.")
    _, body, _ = call(
        "POST",
        f"{KEYCLOAK_URL}/realms/master/protocol/openid-connect/token",
        form={"grant_type": "password", "client_id": "admin-cli", "username": user, "password": password},
    )
    return body["access_token"]


def demo_user_token():
    _, body, _ = call(
        "POST",
        f"{KEYCLOAK_URL}/realms/{REALM}/protocol/openid-connect/token",
        form={
            "grant_type": "password",
            "client_id": CLIENT_ID,
            "username": DEMO_EMAIL,
            "password": DEMO_PASSWORD,
            "scope": "openid",
        },
    )
    return body["access_token"]


class Keycloak:
    def __init__(self, token):
        self.token = token
        self.base = f"{KEYCLOAK_URL}/admin/realms/{REALM}"

    def req(self, method, path, body=None, expect=(200, 201, 204)):
        return call(method, f"{self.base}{path}", token=self.token, body=body, expect=expect)[1]

    def find_user(self, email):
        users = self.req("GET", f"/users?email={urllib.parse.quote(email)}&exact=true")
        return users[0] if users else None

    def realm_roles(self, user_id):
        return self.req("GET", f"/users/{user_id}/role-mappings/realm")

    def set_realm_roles(self, user_id, wanted):
        """Leaves the user with exactly the `wanted` realm roles."""
        current = {r["name"]: r for r in self.realm_roles(user_id)}
        extra = [r for name, r in current.items() if name not in wanted]
        if extra:
            self.req("DELETE", f"/users/{user_id}/role-mappings/realm", extra)
        missing = [name for name in wanted if name not in current]
        if missing:
            roles = [self.req("GET", f"/roles/{name}") for name in missing]
            self.req("POST", f"/users/{user_id}/role-mappings/realm", roles)


def register_demo_customer():
    print("Registering the demo customer ...")
    call(
        "POST",
        f"{GATEWAY_URL}/api/customers/register",
        body={
            "firstName": "Alex",
            "lastName": "Morgan",
            "email": DEMO_EMAIL,
            "password": DEMO_PASSWORD,
            "phoneNumber": "+15550100199",
            "dateOfBirth": "1990-04-12",
            "address": {
                "street": "100 Market Street",
                "city": "San Francisco",
                "state": "CA",
                "zipCode": "94105",
                "country": "US",
            },
        },
    )


def seed_banking_data(token):
    def api(method, path, body=None, idempotent=False):
        headers = {"Idempotency-Key": str(uuid.uuid4())} if idempotent else None
        return call(method, f"{GATEWAY_URL}{path}", token=token, body=body, headers=headers)[1]

    accounts = api("GET", "/api/accounts/me")
    if accounts:
        print(f"Demo account already has {len(accounts)} account(s); leaving its data alone.")
        return

    print("Opening accounts ...")
    checking = api("POST", "/api/accounts", {"type": 0, "currency": "USD"})
    savings = api("POST", "/api/accounts", {"type": 1, "currency": "USD"})
    chk, sav = checking["id"], savings["id"]

    print("Posting transactions ...")
    deposit = lambda acct, amount, note: api(
        "POST", "/api/transactions/deposit", {"accountId": acct, "amount": amount, "description": note}, True)
    withdraw = lambda acct, amount, note: api(
        "POST", "/api/transactions/withdraw", {"accountId": acct, "amount": amount, "description": note}, True)
    transfer = lambda src, dst, amount, note: api(
        "POST", "/api/transactions/transfer",
        {"fromAccountId": src, "toAccountId": dst, "amount": amount, "description": note}, True)

    deposit(chk, 4200.00, "Payroll - Northwind Labs")
    deposit(sav, 12000.00, "Opening deposit")
    withdraw(chk, 1850.00, "Rent - March")
    withdraw(chk, 126.40, "Groceries - Fresh Market")
    withdraw(chk, 84.15, "Electric bill")
    withdraw(chk, 42.99, "Internet")
    transfer(chk, sav, 500.00, "Monthly savings")
    withdraw(chk, 63.20, "Dinner out")
    deposit(chk, 4200.00, "Payroll - Northwind Labs")
    deposit(sav, 18.40, "Interest")
    withdraw(chk, 29.99, "Streaming subscription")
    transfer(sav, chk, 250.00, "Cover travel booking")

    print("Issuing cards ...")
    api("POST", "/api/cards", {"accountId": chk, "type": "Debit", "spendingLimit": 2500})
    credit = api("POST", "/api/cards", {"accountId": chk, "type": "Credit", "spendingLimit": 5000})
    # One frozen card so the Cards page shows more than one state.
    api("POST", f"/api/cards/{credit['id']}/freeze")

    # Statements read the ledger, which is filled in asynchronously from events.
    print("Waiting for the ledger to catch up ...")
    time.sleep(8)

    print("Generating a statement ...")
    now = datetime.now(timezone.utc)
    period_start = now.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    api("POST", "/api/statements/generate", {
        "accountId": chk,
        "periodStart": period_start.isoformat().replace("+00:00", "Z"),
        "periodEnd": now.isoformat().replace("+00:00", "Z"),
    })


def lock_down(kc, user_id):
    print("Locking the account down ...")
    # Exactly one role. Dropping Keycloak's default roles also drops the account-console
    # roles (manage-account), so the shared password can't be changed from the UI.
    kc.set_realm_roles(user_id, [DEMO_ROLE])
    kc.req("PUT", f"/users/{user_id}/reset-password",
           {"type": "password", "value": DEMO_PASSWORD, "temporary": False})
    # Keycloak's user-profile validation wants the full representation back on a PUT, so
    # fetch it and change only what we mean to.
    rep = kc.req("GET", f"/users/{user_id}")
    rep.update({"enabled": True, "emailVerified": True, "requiredActions": []})
    kc.req("PUT", f"/users/{user_id}", rep)
    # Clears any brute-force lockout from someone hammering the public login.
    kc.req("DELETE", f"/attack-detection/brute-force/users/{user_id}")


def verify_read_only():
    print("Verifying the account really is read-only ...")
    token = demo_user_token()
    status, _, _ = call("GET", f"{GATEWAY_URL}/api/accounts/me", token=token, expect=None)
    if status != 200:
        sys.exit(f"Demo user could not read its accounts (HTTP {status}).")
    status, _, _ = call(
        "POST", f"{GATEWAY_URL}/api/accounts", token=token,
        body={"type": 0, "currency": "USD"}, expect=None)
    if status != 403:
        sys.exit(f"Expected the gateway to refuse a write with 403, got HTTP {status}. "
                 "Is the gateway running the demo read-only build?")
    print("OK: reads succeed, writes are refused with 403.")


def main():
    kc = Keycloak(admin_token())

    # The role has to exist before the user does: without it the account would be left as
    # an ordinary customer whose password is public. A fresh realm import already has it;
    # a long-lived realm doesn't, so create it here rather than asking for a manual step.
    status, _, _ = call("GET", f"{kc.base}/roles/{DEMO_ROLE}", token=kc.token, expect=None)
    if status == 404:
        print(f"Creating realm role '{DEMO_ROLE}' ...")
        kc.req("POST", "/roles", {
            "name": DEMO_ROLE,
            "description": "Public demo account. The gateway rejects every non-GET request "
                           "for tokens carrying this role.",
        })
    elif status != 200:
        sys.exit(f"Could not check for realm role '{DEMO_ROLE}' (HTTP {status}).")

    user = kc.find_user(DEMO_EMAIL)
    if user is None:
        register_demo_customer()
        user = kc.find_user(DEMO_EMAIL)
    user_id = user["id"]

    # The role would block the writes seeding needs, so take it off for the duration.
    # Passwords are reset first so the token request below works on a re-run.
    kc.req("PUT", f"/users/{user_id}/reset-password",
           {"type": "password", "value": DEMO_PASSWORD, "temporary": False})
    kc.req("DELETE", f"/attack-detection/brute-force/users/{user_id}")
    current = [r["name"] for r in kc.realm_roles(user_id)]
    if DEMO_ROLE in current:
        kc.set_realm_roles(user_id, [r for r in current if r != DEMO_ROLE])

    seed_banking_data(demo_user_token())
    lock_down(kc, user_id)
    verify_read_only()
    print(f"\nDone. Demo login: {DEMO_EMAIL} / {DEMO_PASSWORD}")


if __name__ == "__main__":
    main()
