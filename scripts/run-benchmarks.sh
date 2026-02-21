#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
dotnet run --project "$REPO_ROOT/benchmarks/Enyim.Caching.Rendezvous.Benchmarks" -c Release -- "$@"
