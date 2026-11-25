#!/bin/sh
# Runs the demo-account seeder on the production VM. See ../run-in-network.sh for how and why.
# Usage (on the VM):   sh ~/atlas-bank/infra/demo/seed-on-vm.sh
exec sh "$(dirname "$0")/../run-in-network.sh" infra/demo/seed_demo.py
