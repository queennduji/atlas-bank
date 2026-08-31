# System health endpoint

A tiny, dependency-free HTTP endpoint exposing host-level system stats
(memory/swap, disk, load average, `docker compose ps` container health) at
`https://atlasbank.dev/_health/system`, so a **cloud** monitoring routine —
which has no access to the VM's SSH private key — can still check on the
box's resource health.

Runs as a host-level systemd service (`atlas-health.service`), not in a
container, so `os.getloadavg()` / `shutil.disk_usage()` / `/proc/meminfo`
reflect true host state. nginx (which does run in a container) reaches it
via the `extra_hosts` entry on the `nginx` service in
`docker-compose.prod.yml` — that entry points `host.docker.internal` at
`atlas-net`'s actual bridge gateway IP directly, not Docker's built-in
`host-gateway` alias, which resolves to the *default* bridge's gateway
regardless of which network the container is actually on and produced a
"Host is unreachable" error here (see the comment on that entry for the
full story, including how to re-derive the IP if the network is ever
recreated).

Getting from nginx to this port also needs a host firewall exception —
Oracle's default OCI iptables ruleset only allows new inbound connections
on 22/80/443, so a rule scoped to the bridge subnet is required:

```bash
sudo iptables -I INPUT 7 -p tcp -s 172.18.0.0/16 --dport 8099 -m state --state NEW -j ACCEPT
sudo netfilter-persistent save
```

(Adjust the subnet and insert position to match `atlas-net`'s actual
gateway/rule order if they've changed — check with
`sudo iptables -S INPUT`.)

## One-time setup on the VM

```bash
# 1. Create the secret token file (not committed — real value lives only here)
sudo tee /etc/atlas-health.env > /dev/null <<'EOF'
ATLAS_HEALTH_TOKEN=<paste the token>
EOF
sudo chmod 600 /etc/atlas-health.env

# 2. Install the systemd unit
sudo cp ~/atlas-bank/infra/health-check/atlas-health.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now atlas-health

# 3. Confirm it's up locally
curl -s -H "X-Health-Token: <paste the token>" http://127.0.0.1:8099/

# 4. After nginx.conf + docker-compose.prod.yml changes are deployed
#    (docker compose -f docker-compose.prod.yml up -d nginx), confirm the
#    public path works too:
curl -s -H "X-Health-Token: <paste the token>" https://atlasbank.dev/_health/system
```

After editing the script (`server.py`), redeploy with:

```bash
sudo systemctl restart atlas-health
```

## Why port 8099 is safe to leave listening on 0.0.0.0

The VM's OCI security list only opens 22/80/443 — 8099 is never reachable
from outside the VM's own network stack no matter what it binds to. The
`X-Health-Token` check is defense-in-depth on top of that, not the primary
control.
