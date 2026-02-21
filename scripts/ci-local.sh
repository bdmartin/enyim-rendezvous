#!/usr/bin/env bash
#
# Runs the same CI checks locally that run in GitHub Actions:
#   restore → Release build (warnings-as-errors) → test with code coverage
#
# Usage:
#   ./scripts/ci-local.sh           # run all checks
#   ./scripts/ci-local.sh --report  # also generate an HTML coverage report (requires reportgenerator)
#
set -euo pipefail

SOLUTION="Enyim.Caching.Rendezvous.sln"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COVERAGE_DIR="$REPO_ROOT/coverage"
GENERATE_REPORT=false

for arg in "$@"; do
  case "$arg" in
    --report) GENERATE_REPORT=true ;;
  esac
done

cd "$REPO_ROOT"

echo "==> Restoring dependencies..."
dotnet restore "$SOLUTION"

echo "==> Building (Release)..."
dotnet build "$SOLUTION" --configuration Release --no-restore

echo "==> Running tests with coverage (Release)..."
dotnet test "$SOLUTION" \
  --configuration Release \
  --no-build \
  --verbosity normal \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE_DIR"

echo "==> All CI checks passed."

# Find the generated coverage file
COVERAGE_FILE=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" -type f | head -1)

if [[ -n "${COVERAGE_FILE:-}" ]]; then
  echo "==> Coverage report: $COVERAGE_FILE"
fi

if [[ "$GENERATE_REPORT" == "true" ]]; then
  if command -v reportgenerator &>/dev/null; then
    echo "==> Generating HTML coverage report..."
    reportgenerator \
      "-reports:$COVERAGE_DIR/**/coverage.cobertura.xml" \
      "-targetdir:$COVERAGE_DIR/report" \
      "-reporttypes:Html"
    echo "==> HTML report: $COVERAGE_DIR/report/index.html"
  else
    echo "==> reportgenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
  fi
fi
