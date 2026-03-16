#!/bin/bash
# Import n8n workflow JSON files via the n8n REST API.
# Usage: ./n8n/import-workflows.sh [n8n_base_url]
#
# Prerequisites:
#   - n8n must be running and healthy
#   - curl and jq must be installed
#   - First-time setup: create an owner account at http://localhost:5678 before running

set -euo pipefail

N8N_URL="${1:-http://localhost:5678}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORKFLOWS_DIR="$SCRIPT_DIR/workflows"

echo "Waiting for n8n to be ready at $N8N_URL ..."
for i in $(seq 1 30); do
  if curl -sf "$N8N_URL/healthz" > /dev/null 2>&1; then
    echo "n8n is ready."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "ERROR: n8n did not become ready within 30 attempts."
    exit 1
  fi
  sleep 2
done

# n8n requires an API key for REST API access.
# Set N8N_API_KEY env var, or the script will prompt for it.
if [ -z "${N8N_API_KEY:-}" ]; then
  echo ""
  echo "To import workflows via API, you need an n8n API key."
  echo "Generate one in n8n UI: Settings > API > Create API Key"
  echo ""
  read -rp "Enter your n8n API key: " N8N_API_KEY
fi

if [ -z "$N8N_API_KEY" ]; then
  echo "ERROR: No API key provided. Aborting."
  exit 1
fi

# Create "Broker API Auth" credential (httpBasicAuth) for login nodes.
# Uses BROKER_API_USERNAME / ADMIN_PASSWORD env vars, or prompts.
create_credential() {
  local user="${BROKER_API_USERNAME:-admin}"
  local pass="${ADMIN_PASSWORD:-}"

  if [ -z "$pass" ]; then
    read -rsp "Enter Broker API password for n8n credential (admin user): " pass
    echo ""
  fi

  echo "Creating 'Broker API Auth' credential ..."

  local payload
  payload=$(jq -n --arg u "$user" --arg p "$pass" \
    '{name:"Broker API Auth",type:"httpBasicAuth",data:{user:$u,password:$p}}')

  local response
  response=$(curl -sf -X POST "$N8N_URL/api/v1/credentials" \
    -H "Content-Type: application/json" \
    -H "X-N8N-API-KEY: $N8N_API_KEY" \
    -d "$payload" 2>&1) || {
    echo "  WARNING: Could not create credential (may already exist): $response"
    return 0
  }

  local cred_id
  cred_id=$(echo "$response" | jq -r '.id')
  echo "  Created credential id: $cred_id"

  # Patch workflow files so credential ID matches the one n8n assigned
  CRED_ID="$cred_id"
}

CRED_ID=""
create_credential

import_workflow() {
  local file="$1"
  local name
  name=$(jq -r '.name' "$file")

  echo "Importing workflow: $name ..."

  # Patch credential IDs to match the one created above
  local import_data
  if [ -n "$CRED_ID" ]; then
    import_data=$(jq --arg id "$CRED_ID" \
      '(.nodes[] | .credentials?.httpBasicAuth?.id?) = $id' "$file")
  else
    import_data=$(cat "$file")
  fi

  local response
  response=$(echo "$import_data" | curl -sf -X POST "$N8N_URL/api/v1/workflows" \
    -H "Content-Type: application/json" \
    -H "X-N8N-API-KEY: $N8N_API_KEY" \
    -d @- 2>&1) || {
    echo "  FAILED to import $name: $response"
    return 1
  }

  local workflow_id
  workflow_id=$(echo "$response" | jq -r '.id')
  echo "  Imported: $name (id: $workflow_id)"
}

echo ""
echo "Importing workflows from $WORKFLOWS_DIR ..."
echo ""

imported=0
failed=0

for workflow_file in "$WORKFLOWS_DIR"/*.json; do
  [ -f "$workflow_file" ] || continue
  if import_workflow "$workflow_file"; then
    imported=$((imported + 1))
  else
    failed=$((failed + 1))
  fi
done

echo ""
echo "Done. Imported: $imported, Failed: $failed"
echo ""
echo "Next steps:"
echo "  1. Open $N8N_URL and review the imported workflows"
echo "  2. Activate workflows you want to run"
echo "  3. Health Check: runs automatically every 5 minutes"
echo "  4. Client Onboarding: POST to $N8N_URL/webhook/client-onboarding"
echo "  5. Transaction Import: POST to $N8N_URL/webhook/import-transactions"
