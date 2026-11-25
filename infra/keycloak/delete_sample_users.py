#!/usr/bin/env python3
"""Deletes the sample users that older copies of keycloak/realm-export.json created.

Those two accounts shipped with passwords that were committed to the repo, so a realm that
was imported from the old file has working logins that anyone could read. The export no
longer contains them; this removes them from a realm that already does. It deletes exactly
these usernames and nothing else, is safe to re-run, and finishes by trying each old login
to confirm it no longer works. (It checks that the accounts are gone by looking them up, not by
logging in: the web client's password grant is closed, so a failed login would prove nothing.)

Required environment:  KC_ADMIN_USER, KC_ADMIN_PASSWORD   (Keycloak master-realm admin)
Optional environment:  KEYCLOAK_URL (default http://localhost:8080), KEYCLOAK_REALM (default atlas-bank)

Run it on the VM with:  sh ~/atlas-bank/infra/run-in-network.sh infra/keycloak/delete_sample_users.py
"""
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request

KEYCLOAK_URL = os.environ.get("KEYCLOAK_URL", "http://localhost:8080").rstrip("/")
REALM = os.environ.get("KEYCLOAK_REALM", "atlas-bank")

# Hard-coded on purpose: this script must never be able to delete anything else.
SAMPLE_USERS = [("testuser", "testuser@atlasbank.com"), ("adminuser", "admin@atlasbank.com")]


def request(method, url, *, token=None, form=None, expect=(200, 204)):
    data = urllib.parse.urlencode(form).encode() if form is not None else None
    headers = {"Accept": "application/json"}
    if form is not None:
        headers["Content-Type"] = "application/x-www-form-urlencoded"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=30) as res:
            status, raw = res.status, res.read()
    except urllib.error.HTTPError as err:
        status, raw = err.code, err.read()
    if expect is not None and status not in expect:
        sys.exit(f"{method} {url} -> HTTP {status}: {raw.decode(errors='replace')[:200]}")
    return status, (json.loads(raw) if raw else None)


def main():
    admin_user, admin_password = os.environ.get("KC_ADMIN_USER"), os.environ.get("KC_ADMIN_PASSWORD")
    if not admin_user or not admin_password:
        sys.exit("Set KC_ADMIN_USER and KC_ADMIN_PASSWORD in the environment first.")

    _, body = request(
        "POST", f"{KEYCLOAK_URL}/realms/master/protocol/openid-connect/token",
        form={"grant_type": "password", "client_id": "admin-cli",
              "username": admin_user, "password": admin_password})
    token = body["access_token"]
    base = f"{KEYCLOAK_URL}/admin/realms/{REALM}"

    def find(query):
        _, users = request("GET", f"{base}/users?{query}&exact=true", token=token)
        return users

    for username, _ in SAMPLE_USERS:
        users = find(f"username={urllib.parse.quote(username)}")
        if not users:
            print(f"{username}: already gone")
            continue
        request("DELETE", f"{base}/users/{users[0]['id']}", token=token)
        print(f"{username}: deleted")

    print("Checking the accounts are really gone ...")
    remaining = []
    for username, email in SAMPLE_USERS:
        found = find(f"username={urllib.parse.quote(username)}") + find(f"email={urllib.parse.quote(email)}")
        print(f"  {username}: {'STILL EXISTS' if found else 'gone'}")
        if found:
            remaining.append(username)
    if remaining:
        sys.exit(f"Still present: {', '.join(remaining)}")
    print("Done.")


if __name__ == "__main__":
    main()
