#!/usr/bin/env bash
#
# Runs the same CI checks locally that run in GitHub Actions:
#   restore → Release build (warnings-as-errors) → test with code coverage
#
# Usage:
#   ./scripts/ci-local.sh             # run checks, print coverage summary
#   ./scripts/ci-local.sh --report    # also generate an HTML coverage report
#   ./scripts/ci-local.sh --no-report # skip coverage report generation
#
set -euo pipefail

SOLUTION="Enyim.Caching.Rendezvous.sln"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COVERAGE_DIR="$REPO_ROOT/coverage"
GENERATE_REPORT=true

for arg in "$@"; do
  case "$arg" in
    --report) GENERATE_REPORT=true ;;
    --no-report) GENERATE_REPORT=false ;;
  esac
done

cd "$REPO_ROOT"

# Ensure local tools (reportgenerator) are available
dotnet tool restore --verbosity quiet

# Clean previous coverage results to avoid stale data
rm -rf "$COVERAGE_DIR"

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
echo ""

# Generate coverage reports
echo "==> Coverage summary:"
dotnet reportgenerator \
  "-reports:$COVERAGE_DIR/**/coverage.cobertura.xml" \
  "-targetdir:$COVERAGE_DIR/report" \
  "-reporttypes:TextSummary;Html" \
  -verbosity:Warning

# Print the text summary to the console
if [[ -f "$COVERAGE_DIR/report/Summary.txt" ]]; then
  cat "$COVERAGE_DIR/report/Summary.txt"
fi

if [[ "$GENERATE_REPORT" == "true" ]]; then
  echo ""
  echo "==> HTML report: $COVERAGE_DIR/report/index.html"
fi
