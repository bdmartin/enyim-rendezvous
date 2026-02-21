# Contributing

Thanks for your interest in contributing to enyim-rendezvous!

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- A memcached instance is **not** required — all tests run without external services

## Workflow

1. Fork the repository and create a feature branch from `main`
2. Make your changes
3. Ensure the build passes with zero warnings:
   ```bash
   dotnet build --configuration Release
   ```
4. Ensure all tests pass:
   ```bash
   dotnet test --configuration Release
   ```
5. Open a pull request against `main`

## Code Style

This project uses an `.editorconfig` to enforce formatting. Most editors will pick it up automatically.

All public types and members require XML documentation comments. The build enables `GenerateDocumentationFile` with `TreatWarningsAsErrors`, so missing comments will cause a build failure (CS1591).

## Reporting Issues

Please use the [issue templates](https://github.com/bdmartin/enyim-rendezvous/issues/new/choose) when filing bugs or feature requests.
