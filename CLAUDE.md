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

# Skip the HTML report (text summary still prints)
./scripts/ci-local.sh --no-report
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

Tests use xUnit with Moq. Hash algorithm tests are parameterized via `[Theory]`/`[InlineData(typeof(...))]` across all three hash types. Distribution tests assert <20% deviation from uniform across 10,000 keys.
