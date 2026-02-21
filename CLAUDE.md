# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET library that implements Rendezvous (Highest Random Weight) hashing as a node locator for EnyimMemcachedCore. Targets `netstandard2.0`. Tests target `net8.0`.

## Build & Test Commands

```bash
# Run all CI checks (restore, Release build, test with coverage summary + HTML report)
./scripts/ci-local.sh

# Run a single test by name
dotnet test tests/Enyim.Caching.Rendezvous.Tests/ --filter "FullyQualifiedName~TestMethodName"

# Run integration tests only
dotnet test tests/Enyim.Caching.Rendezvous.IntegrationTests/ -c Release

# Skip the HTML report (text summary still prints)
./scripts/ci-local.sh --no-report

# Run benchmarks (standalone, not in solution)
./scripts/run-benchmarks.sh                          # all benchmarks
./scripts/run-benchmarks.sh --filter '*HashAlgorithm*'  # filter by class

# Run hash quality profiler (standalone, not in solution)
./scripts/run-profiler.sh                            # console output
./scripts/run-profiler.sh --format csv --output-dir profiler-results
./scripts/run-profiler.sh --format json --output-dir profiler-results
```

The pre-push hook (`.githooks/pre-push`) also runs `scripts/ci-local.sh` automatically. Configure it with: `git config core.hooksPath .githooks`.

## Architecture

The library plugs into EnyimMemcachedCore via two interfaces: `IMemcachedNodeLocator` and `IProviderFactory<IMemcachedNodeLocator>`.

**Core flow:** `RendezvousNodeLocatorFactory` creates `RendezvousNodeLocator` instances, each injected with an `IRendezvousHash` strategy. When `Locate(key)` is called, the locator scores every alive node using the hash and returns the highest-scoring one.

**Key types:**
- `IRendezvousHash` — strategy interface: `uint ComputeHash(string key, string node)`. Highest score wins.
- `RendezvousNodeLocator` — thread-safe (uses `ReaderWriterLockSlim`) locator that implements `IMemcachedNodeLocator`.
- `RendezvousNodeLocatorFactory` — creates locator instances sharing a single hash implementation.

**Hash algorithms** (in `Hashing/`):
- `FnvRendezvousHash` — FNV-1a 32-bit (default)
- `MurmurHash3RendezvousHash` — MurmurHash3 x86 32-bit, configurable seed
- `Sha256RendezvousHash` — SHA-256, takes first 4 bytes as uint32

**ElastiCache auto-discovery** (in `ElastiCache/`):
- `ElastiCacheDiscoveryService` — background timer polls `config get cluster` endpoint; fires `NodesChanged` event only when config version changes.
- `ClusterConfigParser` — parses the wire protocol response into `ClusterNode` records.

## Testing

Tests use xUnit with Moq across two projects:

- **Unit tests** (`tests/Enyim.Caching.Rendezvous.Tests/`) — hash algorithm correctness, locator behavior, factory, parser, and discovery service. Hash algorithm tests are parameterized via `[Theory]`/`[InlineData(typeof(...))]` across all three hash types. Distribution tests assert <20% deviation from uniform across 10,000 keys.
- **Integration tests** (`tests/Enyim.Caching.Rendezvous.IntegrationTests/`) — end-to-end factory-to-locator pipeline, topology change monotonicity (node add/remove), concurrent read/write thread safety, and ElastiCache discovery-to-locator flow.

Both projects are in the solution and run together via `dotnet test` and `ci-local.sh`.

## Benchmarks & Profiler

These are standalone projects (not in the solution) for on-demand performance and quality analysis.

- **Benchmarks** (`benchmarks/Enyim.Caching.Rendezvous.Benchmarks/`) — BenchmarkDotNet suite covering hash algorithm throughput, locator scaling by node count, and key pattern impact. Run via `scripts/run-benchmarks.sh`.
- **Hash profiler** (`tools/Enyim.Caching.Rendezvous.HashProfiler/`) — profiles all three hash algorithms across four quality metrics: chi-squared distribution uniformity, avalanche bit-flip diffusion, HRW monotonicity guarantees, and collision rates. Supports console, CSV, and JSON output. Run via `scripts/run-profiler.sh`.
