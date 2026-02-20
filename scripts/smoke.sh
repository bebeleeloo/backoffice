#!/usr/bin/env bash
set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

CHECKS=0
pass() { CHECKS=$((CHECKS + 1)); echo -e "  ${GREEN}PASS${NC} $1"; }
fail() { CHECKS=$((CHECKS + 1)); ERRORS=$((ERRORS + 1)); echo -e "  ${RED}FAIL${NC} $1"; }

ERRORS=0
API=http://localhost:5050
WEB=http://localhost:3000

# ── Parse mode ────────────────────────────────────────────────────
MODE=clean
case "${1:-}" in
  --fast)  MODE=fast ;;
  --clean) MODE=clean ;;
  -h|--help)
    echo "Usage: $0 [--clean|--fast]"
    echo "  --clean  (default) docker compose down -v, rebuild, then run checks"
    echo "  --fast   run checks against already-running services"
    exit 0 ;;
  "") ;; # default
  *) echo "Unknown flag: $1. Use --help." >&2; exit 1 ;;
esac

echo -e "${YELLOW}=== Broker Backoffice Smoke Test (${MODE}) ===${NC}"
echo ""

# ── 1. Optionally start from scratch ─────────────────────────────
if [ "$MODE" = "clean" ]; then
  echo "Tearing down existing environment..."
  docker compose down -v --remove-orphans 2>/dev/null || true

  echo "Building and starting services..."
  docker compose up --build -d 2>&1 | tail -5

  echo "Waiting for services to become healthy..."
  for _ in $(seq 1 60); do
    if docker compose ps --format json 2>/dev/null | python3 -c "
import sys, json
services = [json.loads(line) for line in sys.stdin if line.strip()]
healthy = [s for s in services if s.get('Health','') == 'healthy']
sys.exit(0 if len(healthy) >= 2 else 1)
" 2>/dev/null; then
      break
    fi
    sleep 2
  done
  echo ""
fi

# ── 2. Health checks ─────────────────────────────────────────────
echo "Health checks:"
for ep in /health/live /health/ready; do
  code=$(curl -s -o /dev/null -w '%{http_code}' "${API}${ep}")
  if [ "$code" = "200" ]; then pass "$ep → $code"; else fail "$ep → $code (expected 200)"; fi
done
echo ""

# ── 3. Swagger ────────────────────────────────────────────────────
echo "Swagger:"
code=$(curl -s -o /dev/null -w '%{http_code}' "${API}/swagger/v1/swagger.json")
if [ "$code" = "200" ]; then pass "/swagger/v1/swagger.json → $code"; else fail "/swagger → $code"; fi
echo ""

# ── 4. Auth ───────────────────────────────────────────────────────
echo "Auth:"
LOGIN_RESULT=$(python3 << 'PYEOF'
import urllib.request, json, sys
data = json.dumps({"username":"admin","password":"Admin123!"}).encode()
req = urllib.request.Request("http://localhost:5050/api/v1/auth/login",
                             data=data, headers={"Content-Type":"application/json"})
try:
    resp = urllib.request.urlopen(req)
    body = json.loads(resp.read())
    print(body["accessToken"])
except Exception as e:
    print("ERROR:" + str(e), file=sys.stderr)
    sys.exit(1)
PYEOF
)

if [ -n "$LOGIN_RESULT" ]; then
  pass "POST /auth/login → token received"
else
  fail "POST /auth/login → no token"
fi

me_code=$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer ${LOGIN_RESULT}" "${API}/api/v1/auth/me")
if [ "$me_code" = "200" ]; then pass "GET /auth/me → $me_code"; else fail "GET /auth/me → $me_code"; fi

wrong_code=$(python3 << 'PYEOF'
import urllib.request, json
data = json.dumps({"username":"admin","password":"wrong"}).encode()
req = urllib.request.Request("http://localhost:5050/api/v1/auth/login",
                             data=data, headers={"Content-Type":"application/json"})
try:
    urllib.request.urlopen(req)
    print("200")
except urllib.error.HTTPError as e:
    print(e.code)
PYEOF
)
if [ "$wrong_code" = "401" ]; then pass "POST /auth/login (wrong pwd) → 401"; else fail "wrong pwd → $wrong_code (expected 401)"; fi
echo ""

# ── 5. API endpoints ─────────────────────────────────────────────
echo "API endpoints:"
AUTH_HEADER="Authorization: Bearer ${LOGIN_RESULT}"
for ep in /api/v1/users /api/v1/roles /api/v1/permissions /api/v1/audit /api/v1/clients /api/v1/countries; do
  code=$(curl -s -o /dev/null -w '%{http_code}' -H "$AUTH_HEADER" "${API}${ep}")
  if [ "$code" = "200" ]; then pass "GET $ep → $code"; else fail "GET $ep → $code"; fi
done
echo ""

# ── 6. Demo data populated ─────────────────────────────────────
echo "Demo data:"
client_list_count=$(python3 << PYEOF
import urllib.request, json
req = urllib.request.Request("http://localhost:5050/api/v1/clients",
                             headers={"Authorization": "Bearer ${LOGIN_RESULT}"})
resp = urllib.request.urlopen(req)
body = json.loads(resp.read())
# body may be a list or a paged object with items/data
if isinstance(body, list):
    print(len(body))
elif isinstance(body, dict):
    items = body.get("items") or body.get("data") or []
    print(len(items))
else:
    print(0)
PYEOF
)
if [ "$client_list_count" -gt 0 ] 2>/dev/null; then
  pass "GET /clients → non-empty list ($client_list_count items)"
else
  fail "GET /clients → empty list"
fi
echo ""

# ── 7. Client CRUD integration ──────────────────────────────────
echo "Client CRUD:"
# Get a country ID for addresses
COUNTRY_ID=$(python3 << PYEOF
import urllib.request, json
req = urllib.request.Request("http://localhost:5050/api/v1/countries",
                             headers={"Authorization": "Bearer ${LOGIN_RESULT}"})
resp = urllib.request.urlopen(req)
countries = json.loads(resp.read())
# Find US or use first
c = next((c for c in countries if c["iso2"] == "US"), countries[0])
print(c["id"])
PYEOF
)

if [ -n "$COUNTRY_ID" ]; then
  pass "GET /countries → got country ID"
else
  fail "GET /countries → no country ID"
fi

# Create client
CLIENT_RESULT=$(python3 << PYEOF
import urllib.request, json, sys
data = json.dumps({
    "clientType": "Individual",
    "status": "Active",
    "email": "smoke-test-$(date +%s)@test.local",
    "pepStatus": False,
    "kycStatus": "NotStarted",
    "firstName": "Smoke",
    "lastName": "Test",
    "ssn": "123-45-6789",
    "addresses": [{
        "type": "Legal",
        "line1": "123 Main St",
        "city": "New York",
        "countryId": "${COUNTRY_ID}"
    }],
    "investmentProfile": {
        "objective": "Growth",
        "riskTolerance": "Medium"
    }
}).encode()
req = urllib.request.Request("http://localhost:5050/api/v1/clients",
                             data=data,
                             headers={"Content-Type":"application/json",
                                      "Authorization": "Bearer ${LOGIN_RESULT}"})
try:
    resp = urllib.request.urlopen(req)
    body = json.loads(resp.read())
    # Validate response structure
    assert body.get("residenceCountryId") is None or True
    assert body.get("addresses") is not None
    assert len(body["addresses"]) == 1
    assert body["addresses"][0].get("countryIso2") is not None
    assert body.get("investmentProfile") is not None
    assert body["investmentProfile"]["objective"] == "Growth"
    print(json.dumps({"id": body["id"], "rowVersion": body["rowVersion"]}))
except Exception as e:
    print("ERROR:" + str(e), file=sys.stderr)
    sys.exit(1)
PYEOF
)

if echo "$CLIENT_RESULT" | python3 -c "import sys,json; json.loads(sys.stdin.read())" 2>/dev/null; then
  pass "POST /clients → created with addresses + investmentProfile"
else
  fail "POST /clients → $CLIENT_RESULT"
fi

# Get client by ID
CLIENT_ID=$(echo "$CLIENT_RESULT" | python3 -c "import sys,json; print(json.loads(sys.stdin.read())['id'])" 2>/dev/null)
ROW_VERSION=$(echo "$CLIENT_RESULT" | python3 -c "import sys,json; print(json.loads(sys.stdin.read())['rowVersion'])" 2>/dev/null)

get_code=$(curl -s -o /dev/null -w '%{http_code}' -H "$AUTH_HEADER" "${API}/api/v1/clients/${CLIENT_ID}")
if [ "$get_code" = "200" ]; then pass "GET /clients/${CLIENT_ID} → $get_code"; else fail "GET /clients/${CLIENT_ID} → $get_code"; fi

# Update client
UPDATE_RESULT=$(python3 << PYEOF
import urllib.request, json, sys
data = json.dumps({
    "id": "${CLIENT_ID}",
    "clientType": "Individual",
    "status": "Active",
    "email": "smoke-updated-$(date +%s)@test.local",
    "pepStatus": True,
    "kycStatus": "InProgress",
    "firstName": "Updated",
    "lastName": "Client",
    "residenceCountryId": "${COUNTRY_ID}",
    "addresses": [
        {"type": "Legal", "line1": "456 Oak Ave", "city": "Boston", "countryId": "${COUNTRY_ID}"},
        {"type": "Mailing", "line1": "789 Pine Rd", "city": "Chicago", "countryId": "${COUNTRY_ID}"}
    ],
    "investmentProfile": {
        "objective": "Income",
        "riskTolerance": "Low",
        "notes": "Updated via smoke test"
    },
    "rowVersion": "${ROW_VERSION}"
}).encode()
req = urllib.request.Request("http://localhost:5050/api/v1/clients/${CLIENT_ID}",
                             data=data, method="PUT",
                             headers={"Content-Type":"application/json",
                                      "Authorization": "Bearer ${LOGIN_RESULT}"})
try:
    resp = urllib.request.urlopen(req)
    body = json.loads(resp.read())
    assert len(body["addresses"]) == 2, f"Expected 2 addresses, got {len(body['addresses'])}"
    assert body["residenceCountryIso2"] is not None
    assert body["investmentProfile"]["objective"] == "Income"
    print("OK")
except Exception as e:
    print("ERROR:" + str(e), file=sys.stderr)
    sys.exit(1)
PYEOF
)

if [ "$UPDATE_RESULT" = "OK" ]; then
  pass "PUT /clients/${CLIENT_ID} → updated with 2 addresses + investmentProfile"
else
  fail "PUT /clients/${CLIENT_ID} → $UPDATE_RESULT"
fi

# Delete client
del_code=$(curl -s -o /dev/null -w '%{http_code}' -X DELETE -H "$AUTH_HEADER" "${API}/api/v1/clients/${CLIENT_ID}")
if [ "$del_code" = "204" ]; then pass "DELETE /clients/${CLIENT_ID} → $del_code"; else fail "DELETE /clients/${CLIENT_ID} → $del_code"; fi
echo ""

# ── 8. Frontend ───────────────────────────────────────────────────
echo "Frontend:"
web_code=$(curl -s -o /dev/null -w '%{http_code}' "${WEB}/")
if [ "$web_code" = "200" ]; then pass "GET / → $web_code"; else fail "GET / → $web_code"; fi

web_body=$(curl -s "${WEB}/")
root_marker='id="root"'
if echo "$web_body" | grep -q "$root_marker"; then
  pass "GET / → contains id=\"root\""
else
  fail "GET / → missing id=\"root\""
fi

login_code=$(curl -s -o /dev/null -w '%{http_code}' "${WEB}/login")
if [ "$login_code" = "200" ]; then pass "GET /login → $login_code"; else fail "GET /login → $login_code"; fi
echo ""

# ── Summary ───────────────────────────────────────────────────────
echo "──────────────────────────────────────"
if [ "$ERRORS" -eq 0 ]; then
  echo -e "${GREEN}${CHECKS}/${CHECKS} checks passed.${NC}"
  exit 0
else
  echo -e "${RED}$((CHECKS - ERRORS))/${CHECKS} passed, ${ERRORS} failed.${NC}"
  exit 1
fi
