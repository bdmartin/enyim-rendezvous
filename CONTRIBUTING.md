# Contributing

Thanks for your interest in contributing to enyim-rendezvous!

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- A memcached instance is **not** required — all tests run without external services

## Setup

After cloning, configure git to use the project's hooks directory:

```bash
git config core.hooksPath .githooks
```

This enables a **pre-push hook** that runs the same CI checks that run in GitHub Actions, so you'll catch failures before pushing.

## Local CI Checks

A single script — `scripts/ci-local.sh` — defines all CI validation steps. The same script is used by:

- **GitHub Actions** (`ci.yml`) — runs on every push/PR to `main`
- **Pre-push hook** (`.githooks/pre-push`) — runs before every `git push`
- **Manual invocation** — run it directly whenever you want

```bash
# Run all CI checks (restore, Release build, test with coverage + summary)
./scripts/ci-local.sh

# Skip the HTML report (text summary still prints)
./scripts/ci-local.sh --no-report
```

The script prints a per-class coverage summary to the console and generates an HTML report in `coverage/report/index.html`. The `reportgenerator` tool is managed as a local dotnet tool — `dotnet tool restore` runs automatically.

## Workflow

1. Fork the repository and create a feature branch from `main`
2. Make your changes
3. Run CI checks locally:
   ```bash
   ./scripts/ci-local.sh
   ```
4. Open a pull request against `main`

## Code Style

This project uses an `.editorconfig` to enforce formatting. Most editors will pick it up automatically.

All public types and members require XML documentation comments. The build enables `GenerateDocumentationFile` with `TreatWarningsAsErrors`, so missing comments will cause a build failure (CS1591).

## Reporting Issues

Please use the [issue templates](https://github.com/bdmartin/enyim-rendezvous/issues/new/choose) when filing bugs or feature requests.
