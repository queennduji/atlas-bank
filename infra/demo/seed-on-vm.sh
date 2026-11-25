#!/bin/sh
# Runs seed_demo.py on the production VM, inside the Docker network.
#
# nginx no longer serves Keycloak's admin API or the master realm to the internet, so the
# seeder can't be run from a laptop any more. From inside the compose network it talks to
# Keycloak and the gateway directly, the same way the services themselves do.
#
# Usage (on the VM, from anywhere):   sh ~/atlas-bank/infra/demo/seed-on-vm.sh
#
# The admin password is read from the repo's .env file by this script, passed to the
# container by variable NAME (so it never shows up in `ps` or shell history), and never
# printed. Set KC_ADMIN_PASSWORD yourself first to skip the .env lookup.
set -eu

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
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
    -v "$ROOT/infra/demo/seed_demo.py:/seed_demo.py:ro" \
    python:3.13-alpine python -I /seed_demo.py
