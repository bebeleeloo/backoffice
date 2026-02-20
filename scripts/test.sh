#!/usr/bin/env bash
set -euo pipefail

YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

FILTER="${1:-all}"

echo -e "${YELLOW}=== Running backend tests in Docker ===${NC}"
echo ""

case "$FILTER" in
  unit)
    PROJECTS="tests/Broker.Backoffice.Tests.Unit"
    ;;
  integration)
    echo "Note: integration tests use Testcontainers and require Docker-in-Docker."
    echo "Mount the Docker socket for them to work."
    PROJECTS="tests/Broker.Backoffice.Tests.Integration"
    ;;
  all)
    PROJECTS="Broker.Backoffice.sln"
    ;;
  *)
    echo "Usage: $0 [unit|integration|all]"
    exit 1
    ;;
esac

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

docker run --rm \
  -v "${REPO_ROOT}/backend:/src/backend" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test "backend/${PROJECTS}" \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal"

STATUS=$?

echo ""
if [ "$STATUS" -eq 0 ]; then
  echo -e "${GREEN}Tests passed.${NC}"
else
  echo -e "${RED}Tests failed (exit code ${STATUS}).${NC}"
fi

exit "$STATUS"
