#!/bin/bash
# One-time bootstrap: nginx needs *some* certificate to start (its config
# references cert paths that must exist), but certbot needs nginx running on
# port 80 to serve the ACME HTTP-01 challenge. This script breaks that
# chicken-and-egg by starting nginx with a throwaway self-signed cert first,
# then swapping in the real Let's Encrypt certificate once issued.
#
# Run once from the repo root on the VM:  bash nginx/init-letsencrypt.sh
# Safe to re-run – it detects an existing certificate and asks before
# replacing it.
set -e

COMPOSE="docker compose -f docker-compose.prod.yml"
DOMAIN="atlasbank.dev"
SANS=(atlasbank.dev api.atlasbank.dev auth.atlasbank.dev)
RSA_KEY_SIZE=4096

# nginx/certbot use a named Docker volume (`certbot-conf`), not a host bind
# mount – every step below must go through `docker compose run` against the
# same `certbot` service so it lands in that same volume, not a stray local
# directory.

if $COMPOSE run --rm --entrypoint sh certbot -c "test -d /etc/letsencrypt/live/$DOMAIN" 2>/dev/null; then
  read -p "Existing certificate found for $DOMAIN. Replace it? (y/N) " decision
  if [ "$decision" != "y" ] && [ "$decision" != "Y" ]; then
    exit 0
  fi
fi

echo "### Creating a throwaway self-signed certificate so nginx can start..."
$COMPOSE run --rm --entrypoint sh certbot -c "\
  mkdir -p /etc/letsencrypt/live/$DOMAIN && \
  openssl req -x509 -nodes -newkey rsa:$RSA_KEY_SIZE -days 1 \
    -keyout /etc/letsencrypt/live/$DOMAIN/privkey.pem \
    -out /etc/letsencrypt/live/$DOMAIN/fullchain.pem \
    -subj /CN=localhost"

echo "### Starting nginx..."
$COMPOSE up -d --force-recreate nginx

echo "### Deleting throwaway certificate..."
$COMPOSE run --rm --entrypoint sh certbot -c "\
  rm -rf /etc/letsencrypt/live/$DOMAIN /etc/letsencrypt/archive/$DOMAIN /etc/letsencrypt/renewal/$DOMAIN.conf"

echo "### Requesting real Let's Encrypt certificate for: ${SANS[*]}..."
domain_args=""
for d in "${SANS[@]}"; do
  domain_args="$domain_args -d $d"
done

$COMPOSE run --rm --entrypoint "\
  certbot certonly --webroot -w /var/www/certbot \
    $domain_args \
    --rsa-key-size $RSA_KEY_SIZE \
    --register-unsafely-without-email \
    --agree-tos \
    --non-interactive" certbot

echo "### Reloading nginx with the real certificate..."
$COMPOSE exec nginx nginx -s reload

echo "### Done. Certbot's own container auto-renews every 12h when due."
