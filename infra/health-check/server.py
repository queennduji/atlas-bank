#!/usr/bin/env python3
"""
Minimal, dependency-free HTTP health endpoint for the Atlas Bank production VM.

Runs directly on the host (NOT in a container) so `os.getloadavg()`,
`shutil.disk_usage()`, and `/proc/meminfo` all reflect true host state rather
than a container's namespaced (and sometimes misleading) view.

Reached two ways:
  - Publicly, via nginx at https://atlasbank.dev/_health/system — nginx
    (itself a container) proxies to this via `host.docker.internal`, wired
    up in docker-compose.prod.yml's `extra_hosts` on the nginx service.
  - Directly from localhost, for debugging.

Auth: a single shared-secret header (X-Health-Token, checked against the
ATLAS_HEALTH_TOKEN env var). This isn't defending against a sophisticated
attacker — the real defense-in-depth is that port 8099 is never opened in
the VM's OCI security list, so nothing external can reach this process
directly regardless of the token. The token just keeps casual/automated
scans of the public path from getting infra internals for free.

Installed as the atlas-health systemd unit (see atlas-health.service in
this same directory) — deploy notes in README.md.
"""
import http.server
import json
import os
import shutil
import subprocess

TOKEN = os.environ.get("ATLAS_HEALTH_TOKEN", "")
PORT = int(os.environ.get("ATLAS_HEALTH_PORT", "8099"))
COMPOSE_FILE = os.environ.get(
    "ATLAS_HEALTH_COMPOSE_FILE", "/home/ubuntu/atlas-bank/docker-compose.prod.yml"
)


def read_meminfo():
    values = {}
    with open("/proc/meminfo") as f:
        for line in f:
            key, _, rest = line.partition(":")
            parts = rest.strip().split()
            if parts:
                values[key] = int(parts[0])  # kB
    return values


def docker_compose_status():
    try:
        result = subprocess.run(
            ["docker", "compose", "-f", COMPOSE_FILE, "ps", "--format", "json"],
            capture_output=True,
            text=True,
            timeout=10,
            check=True,
        )
    except Exception as e:
        return {"error": str(e)}

    containers = []
    # `docker compose ps --format json` emits one JSON object per line.
    for line in result.stdout.strip().splitlines():
        if not line.strip():
            continue
        c = json.loads(line)
        containers.append(
            {
                "name": c.get("Name"),
                "state": c.get("State"),
                "health": c.get("Health", ""),
            }
        )
    return containers


class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path != "/":
            self.send_response(404)
            self.end_headers()
            return

        if not TOKEN or self.headers.get("X-Health-Token") != TOKEN:
            self.send_response(401)
            self.end_headers()
            return

        mem = read_meminfo()
        disk = shutil.disk_usage("/")
        load1, load5, load15 = os.getloadavg()

        body = json.dumps(
            {
                "memory": {
                    "totalMb": mem.get("MemTotal", 0) // 1024,
                    "availableMb": mem.get("MemAvailable", 0) // 1024,
                    "swapTotalMb": mem.get("SwapTotal", 0) // 1024,
                    "swapFreeMb": mem.get("SwapFree", 0) // 1024,
                },
                "disk": {
                    "totalGb": round(disk.total / 1e9, 1),
                    "usedGb": round(disk.used / 1e9, 1),
                    "usedPercent": round(disk.used / disk.total * 100, 1),
                },
                "loadAvg": {"1m": load1, "5m": load5, "15m": load15},
                "containers": docker_compose_status(),
            }
        ).encode()

        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, format, *args):
        pass  # keep the systemd journal quiet; nginx already logs the proxied hit


if __name__ == "__main__":
    http.server.ThreadingHTTPServer(("0.0.0.0", PORT), Handler).serve_forever()
