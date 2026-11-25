#!/bin/sh
# Runs one of the repo's Keycloak/gateway helper scripts on the production VM, inside the
# Docker network.
#
# nginx doesn't serve Keycloak's admin API or the master realm to the internet, so these
# scripts can't be run from a laptop against the public hostname. From inside the compose
# network they talk to Keycloak and the gateway directly, the same way the services do.
#
# Usage (on the VM):   sh ~/atlas-bank/infra/run-in-network.sh infra/demo/seed_demo.py
#
# The admin password is read from the repo's .env by this script, passed to the container
# by variable NAME (so it never shows up in `ps` or shell history), and never printed. Set
# KC_ADMIN_PASSWORD yourself first to skip the .env lookup.
set -eu

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SCRIPT="${1:?usage: run-in-network.sh <path to a .py script, relative to the repo root>}"
[ -f "$ROOT/$SCRIPT" ] || { echo "No such script: $ROOT/$SCRIPT" >&2; exit 1; }
NETWORK="${NETWORK:-atlas-bank_atlas-net}"

if [ -z "${KC_ADMIN_PASSWORD:-}" ]; then
    [ -f "$ROOT/.env" ] || { echo "No $ROOT/.env and KC_ADMIN_PASSWORD isn't set." >&2; exit 1; }
    # Everything after the first '=', minus one pair of surrounding quotes if present.
    KC_ADMIN_PASSWORD="$(grep -E '^KEYCLOAK_ADMIN_PASSWORD=' "$ROOT/.env" | head -n 1 | cut -d= -f2- | sed -e 's/^"\(.*\)"$/\1/' -e "s/^'\(.*\)'\$/\1/")"
    [ -n "$KC_ADMIN_PASSWORD" ] || { echo "KEYCLOAK_ADMIN_PASSWORD not found in $ROOT/.env." >&2; exit 1; }
fi
export KC_ADMIN_PASSWORD

docker network inspect "$NETWORK" >/dev/null 2>&1 || {
    echo "Docker network '$NETWORK' not found. Set NETWORK=<name> (see: docker network ls)." >&2
    exit 1
}

exec docker run --rm \
    --network "$NETWORK" \
    -e KC_ADMIN_PASSWORD \
    -e KC_ADMIN_USER="${KC_ADMIN_USER:-admin}" \
    -e KEYCLOAK_URL="${KEYCLOAK_URL:-http://keycloak:8080}" \
    -e GATEWAY_URL="${GATEWAY_URL:-http://api-gateway:8080}" \
    -v "$ROOT/$SCRIPT:/script.py:ro" \
    python:3.13-alpine python -I /script.py
