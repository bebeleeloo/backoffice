#!/bin/bash
# Import n8n workflows and credentials via docker exec + n8n CLI.
# Usage: ./n8n/import-workflows.sh
#
# Prerequisites:
#   - Docker Compose services must be running (docker compose up -d)
#   - n8n container must be healthy
#
# Environment variables (optional):
#   BROKER_API_USERNAME  — API username for credential (default: admin)
#   ADMIN_PASSWORD       — API password for credential (default: Admin123!)
#   N8N_CONTAINER        — n8n container name (default: broker-n8n)

set -euo pipefail

N8N_CONTAINER="${N8N_CONTAINER:-broker-n8n}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORKFLOWS_DIR="$SCRIPT_DIR/workflows"
BROKER_USER="${BROKER_API_USERNAME:-admin}"
BROKER_PASS="${ADMIN_PASSWORD:-Admin123!}"

# --- Wait for n8n to be healthy ---
echo "Waiting for n8n container '$N8N_CONTAINER' to be healthy ..."
for i in $(seq 1 30); do
  status=$(docker inspect --format='{{.State.Health.Status}}' "$N8N_CONTAINER" 2>/dev/null || echo "not found")
  if [ "$status" = "healthy" ]; then
    echo "n8n is healthy."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "ERROR: n8n did not become healthy within 60s (status: $status)."
    exit 1
  fi
  sleep 2
done

# --- Create temp dir inside container ---
docker exec "$N8N_CONTAINER" mkdir -p /tmp/n8n-import

# --- Copy workflow files into container ---
echo ""
echo "Copying workflow files to container ..."
for f in "$WORKFLOWS_DIR"/*.json; do
  [ -f "$f" ] || continue
  docker cp "$f" "$N8N_CONTAINER:/tmp/n8n-import/$(basename "$f")"
  echo "  Copied $(basename "$f")"
done

# --- Import credentials (httpBasicAuth for Broker API) ---
echo ""
echo "Creating 'Broker API Auth' credential ..."

# Build credential JSON (array format required by n8n CLI)
CRED_JSON=$(cat <<EOF
[{
  "id": "1",
  "name": "Broker API Auth",
  "type": "httpBasicAuth",
  "data": {
    "user": "$BROKER_USER",
    "password": "$BROKER_PASS"
  }
}]
EOF
)

# Write credential to container and import
echo "$CRED_JSON" | docker exec -i "$N8N_CONTAINER" sh -c 'cat > /tmp/n8n-import/credentials.json'
docker exec "$N8N_CONTAINER" n8n import:credentials --input=/tmp/n8n-import/credentials.json 2>/dev/null && \
  echo "  Credential imported." || \
  echo "  WARNING: Credential import failed (may already exist)."

# --- Import workflows ---
echo ""
echo "Importing workflows ..."
docker exec "$N8N_CONTAINER" n8n import:workflow --separate --input=/tmp/n8n-import/ 2>/dev/null && \
  echo "  Workflows imported." || {
  echo "  ERROR: Workflow import failed."
  exit 1
}

# --- Activate workflows (client-onboarding and transaction-import) ---
echo ""
echo "Activating workflows ..."

# List workflows to find IDs
WORKFLOW_LIST=$(docker exec "$N8N_CONTAINER" n8n list:workflow 2>/dev/null || true)
echo "$WORKFLOW_LIST"

# Activate by name — find ID from list output (format: "│ id │ name │ ...")
activate_workflow() {
  local name="$1"
  local wf_id
  wf_id=$(echo "$WORKFLOW_LIST" | grep "$name" | head -1 | awk -F'│' '{gsub(/[ \t]+/,"",$2); print $2}')
  if [ -n "$wf_id" ]; then
    docker exec "$N8N_CONTAINER" n8n update:workflow --id="$wf_id" --active=true 2>/dev/null && \
      echo "  Activated: $name (id: $wf_id)" || \
      echo "  WARNING: Could not activate $name"
  else
    echo "  WARNING: Workflow '$name' not found in list"
  fi
}

activate_workflow "Broker Health Check"
activate_workflow "Client Onboarding"
activate_workflow "Transaction Import"

# --- Cleanup temp files ---
docker exec "$N8N_CONTAINER" rm -rf /tmp/n8n-import

# --- Restart n8n to pick up activated workflows ---
echo ""
echo "Restarting n8n container ..."
docker restart "$N8N_CONTAINER" > /dev/null
echo "  Restarted."

echo ""
echo "Done! Workflows imported and activated."
echo ""
echo "Endpoints:"
echo "  Health Check:        runs every 5 minutes"
echo "  Client Onboarding:   POST http://localhost:5678/webhook/client-onboarding"
echo "  Transaction Import:  POST http://localhost:5678/webhook/import-transactions"
echo ""
echo "Test commands:"
echo '  curl -X POST http://localhost:5678/webhook/client-onboarding \'
echo '    -H "Content-Type: application/json" \'
echo '    -d '\''{"firstName":"Test","lastName":"User","email":"test@example.com"}'\'''
echo ""
echo '  curl -X POST http://localhost:5678/webhook/import-transactions \'
echo '    -H "Content-Type: text/plain" \'
echo '    --data-binary @n8n/test-data/transactions.csv'
