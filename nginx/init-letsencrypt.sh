#!/bin/bash
# One-time bootstrap: nginx needs *some* certificate to start (its config
# references cert paths that must exist), but certbot needs nginx running on
# port 80 to serve the ACME HTTP-01 challenge. This script breaks that
# chicken-and-egg by starting nginx with a throwaway self-signed cert first,
# then swapping in the real Let's Encrypt certificate once issued.
#
# Run once from the repo root on the VM:  bash nginx/init-letsencrypt.sh
# Safe to re-run — it detects an existing certificate and asks before
# replacing it.
set -e

COMPOSE="docker compose -f docker-compose.prod.yml"
DOMAIN="atlasbank.dev"
SANS=(atlasbank.dev api.atlasbank.dev auth.atlasbank.dev)
RSA_KEY_SIZE=4096

if [ -d "./certbot-conf/live/$DOMAIN" ]; then
  read -p "Existing certificate found for $DOMAIN. Replace it? (y/N) " decision
  if [ "$decision" != "y" ] && [ "$decision" != "Y" ]; then
    exit 0
  fi
fi

echo "### Creating a throwaway self-signed certificate so nginx can start..."
mkdir -p "./certbot-conf/live/$DOMAIN"
docker run --rm \
  -v "$(pwd)/certbot-conf:/etc/letsencrypt" \
  --entrypoint openssl certbot/certbot \
  req -x509 -nodes -newkey rsa:$RSA_KEY_SIZE -days 1 \
    -keyout "/etc/letsencrypt/live/$DOMAIN/privkey.pem" \
    -out "/etc/letsencrypt/live/$DOMAIN/fullchain.pem" \
    -subj "/CN=localhost"

echo "### Starting nginx..."
$COMPOSE up -d --force-recreate nginx

echo "### Deleting throwaway certificate..."
rm -rf "./certbot-conf/live/$DOMAIN" "./certbot-conf/archive/$DOMAIN" "./certbot-conf/renewal/$DOMAIN.conf"

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
