#!/usr/bin/env bash
set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

pass() { echo -e "  ${GREEN}PASS${NC} $1"; }
fail() { echo -e "  ${RED}FAIL${NC} $1"; ERRORS=$((ERRORS + 1)); }

ERRORS=0
CONTAINER=broker-postgres
DB=BrokerBackoffice

# Read PG_PASSWORD from .env if present
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${SCRIPT_DIR}/../.env"
PG_PWD=""
if [ -f "$ENV_FILE" ]; then
  PG_PWD=$(grep -E '^\s*PG_PASSWORD\s*=' "$ENV_FILE" \
    | head -1 \
    | sed -E "s/^\s*PG_PASSWORD\s*=\s*//; s/^[\"']//; s/[\"']\s*$//" )
fi
PG_PWD="${PG_PWD:-Your_Strong_Password123}"

psql_query() {
  docker exec -e PGPASSWORD="$PG_PWD" "$CONTAINER" \
    psql -U postgres -d "$DB" -t -A -c "$1" 2>/dev/null | tr -d '\r' | sed '/^$/d'
}

echo -e "${YELLOW}=== Database Health Check ===${NC}"
echo ""

# ── 1. Check container is running ─────────────────────────────────
echo "Container:"
if docker ps --format '{{.Names}}' | grep -q "^${CONTAINER}$"; then
  pass "$CONTAINER is running"
else
  fail "$CONTAINER is not running"
  echo -e "${RED}Cannot continue without database container.${NC}"
  exit 1
fi
echo ""

# ── 2. Required tables ────────────────────────────────────────────
echo "Tables:"
EXPECTED_TABLES="Users Roles Permissions UserRoles RolePermissions UserPermissionOverrides DataScopes UserRefreshTokens AuditLogs Clients ClientAddresses Countries InvestmentProfiles"
existing=$(psql_query "SELECT table_schema || '.' || table_name FROM information_schema.tables WHERE table_schema IN ('public','auth') AND table_type='BASE TABLE' ORDER BY table_name")

for tbl in $EXPECTED_TABLES; do
  if echo "$existing" | grep -q "$tbl"; then
    pass "Table $tbl exists"
  else
    fail "Table $tbl missing"
  fi
done
echo ""

# ── 3. Admin user ─────────────────────────────────────────────────
echo "Seed data:"
admin_count=$(psql_query "SELECT COUNT(*) FROM auth.\"Users\" WHERE \"Username\"='admin'")
if [ "$admin_count" = "1" ]; then pass "Admin user exists"; else fail "Admin user missing (count=$admin_count)"; fi

admin_active=$(psql_query "SELECT CASE WHEN \"IsActive\" THEN 1 ELSE 0 END FROM auth.\"Users\" WHERE \"Username\"='admin'")
if [ "$admin_active" = "1" ]; then pass "Admin user is active"; else fail "Admin user is inactive"; fi

# ── 4. Admin role ──────────────────────────────────────────────────
admin_role=$(psql_query "SELECT COUNT(*) FROM auth.\"Roles\" WHERE \"Name\"='Admin' AND \"IsSystem\"=true")
if [ "$admin_role" = "1" ]; then pass "Admin role exists (system)"; else fail "Admin role missing"; fi

# ── 5. Admin has Admin role ────────────────────────────────────────
has_role=$(psql_query "SELECT COUNT(*) FROM auth.\"UserRoles\" ur JOIN auth.\"Users\" u ON u.\"Id\"=ur.\"UserId\" JOIN auth.\"Roles\" r ON r.\"Id\"=ur.\"RoleId\" WHERE u.\"Username\"='admin' AND r.\"Name\"='Admin'")
if [ "$has_role" = "1" ]; then pass "Admin user has Admin role"; else fail "Admin user missing Admin role"; fi

# ── 6. Permissions count ──────────────────────────────────────────
perm_count=$(psql_query "SELECT COUNT(*) FROM auth.\"Permissions\"")
if [ "$perm_count" -ge 14 ] 2>/dev/null; then
  pass "Permissions seeded ($perm_count)"
else
  fail "Permissions count unexpected ($perm_count)"
fi

# ── 7. Countries seeded ──────────────────────────────────────────
country_count=$(psql_query "SELECT COUNT(*) FROM \"Countries\"")
if [ "$country_count" -ge 200 ] 2>/dev/null; then
  pass "Countries seeded ($country_count)"
else
  fail "Countries count unexpected ($country_count, expected >= 200)"
fi

# ── 8. Countries Iso2 uniqueness ─────────────────────────────────
dup_iso2=$(psql_query "SELECT COUNT(*) FROM (SELECT \"Iso2\" FROM \"Countries\" GROUP BY \"Iso2\" HAVING COUNT(*)>1) t")
if [ "$dup_iso2" = "0" ]; then pass "Countries Iso2 all unique"; else fail "Countries has $dup_iso2 duplicate Iso2 values"; fi

# ── 9. FK Clients → Countries ───────────────────────────────────
orphan_res=$(psql_query "SELECT COUNT(*) FROM \"Clients\" c WHERE c.\"ResidenceCountryId\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"Countries\" co WHERE co.\"Id\"=c.\"ResidenceCountryId\")")
if [ "$orphan_res" = "0" ]; then pass "FK Clients.ResidenceCountryId valid"; else fail "Orphan ResidenceCountryId rows: $orphan_res"; fi

orphan_cit=$(psql_query "SELECT COUNT(*) FROM \"Clients\" c WHERE c.\"CitizenshipCountryId\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"Countries\" co WHERE co.\"Id\"=c.\"CitizenshipCountryId\")")
if [ "$orphan_cit" = "0" ]; then pass "FK Clients.CitizenshipCountryId valid"; else fail "Orphan CitizenshipCountryId rows: $orphan_cit"; fi

# ── 10. FK ClientAddresses → Countries ──────────────────────────
orphan_addr=$(psql_query "SELECT COUNT(*) FROM \"ClientAddresses\" ca WHERE NOT EXISTS (SELECT 1 FROM \"Countries\" co WHERE co.\"Id\"=ca.\"CountryId\")")
if [ "$orphan_addr" = "0" ]; then pass "FK ClientAddresses.CountryId valid"; else fail "Orphan ClientAddresses.CountryId rows: $orphan_addr"; fi

# ── 11. InvestmentProfiles 1:1 constraint ───────────────────────
dup_ip=$(psql_query "SELECT COUNT(*) FROM (SELECT \"ClientId\" FROM \"InvestmentProfiles\" GROUP BY \"ClientId\" HAVING COUNT(*)>1) t")
if [ "$dup_ip" = "0" ]; then pass "InvestmentProfiles 1:1 constraint ok"; else fail "InvestmentProfiles has $dup_ip clients with multiple profiles"; fi

# ── 12. Admin role has all permissions ──────────────────────────────
role_perm_count=$(psql_query "SELECT COUNT(*) FROM auth.\"RolePermissions\" rp JOIN auth.\"Roles\" r ON r.\"Id\"=rp.\"RoleId\" WHERE r.\"Name\"='Admin'")
if [ "$role_perm_count" = "$perm_count" ]; then
  pass "Admin role has all $perm_count permissions"
else
  fail "Admin role has $role_perm_count/$perm_count permissions"
fi

# ── 13. Demo data ──────────────────────────────────────────────────
echo ""
echo "Demo data:"
user_count=$(psql_query "SELECT COUNT(*) FROM auth.\"Users\"")
if [ "$user_count" -ge 10 ] 2>/dev/null; then
  pass "Users seeded ($user_count, expected >= 10)"
else
  fail "Users count too low ($user_count, expected >= 10)"
fi

role_count=$(psql_query "SELECT COUNT(*) FROM auth.\"Roles\"")
if [ "$role_count" -ge 4 ] 2>/dev/null; then
  pass "Roles seeded ($role_count, expected >= 4)"
else
  fail "Roles count too low ($role_count, expected >= 4)"
fi

client_count=$(psql_query "SELECT COUNT(*) FROM \"Clients\"")
if [ "$client_count" -ge 50 ] 2>/dev/null; then
  pass "Clients seeded ($client_count, expected >= 50)"
else
  fail "Clients count too low ($client_count, expected >= 50)"
fi

addr_count=$(psql_query "SELECT COUNT(*) FROM \"ClientAddresses\"")
if [ "$addr_count" -ge 50 ] 2>/dev/null; then
  pass "ClientAddresses seeded ($addr_count, expected >= 50)"
else
  fail "ClientAddresses count too low ($addr_count, expected >= 50)"
fi

ip_count=$(psql_query "SELECT COUNT(*) FROM \"InvestmentProfiles\"")
if [ "$ip_count" -ge 20 ] 2>/dev/null; then
  pass "InvestmentProfiles seeded ($ip_count, expected >= 20)"
else
  fail "InvestmentProfiles count too low ($ip_count, expected >= 20)"
fi

individual_count=$(psql_query "SELECT COUNT(*) FROM \"Clients\" WHERE \"ClientType\"=0")
corporate_count=$(psql_query "SELECT COUNT(*) FROM \"Clients\" WHERE \"ClientType\"=1")
pass "Client mix: $individual_count Individual, $corporate_count Corporate"
echo ""

# ── Summary ────────────────────────────────────────────────────────
if [ "$ERRORS" -eq 0 ]; then
  echo -e "${GREEN}All database checks passed.${NC}"
else
  echo -e "${RED}${ERRORS} check(s) failed.${NC}"
  exit 1
fi
