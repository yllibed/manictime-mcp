# CI Setup (GitHub Actions)

## Goal

- Keep a reproducible CI baseline that matches local validation.
- Enforce a zero-warning target as much as practical.
- Validate package production on every change.

## Required CI Command Chain

```bash
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
dotnet pack src/ManicTimeMcp.slnx -c Release
```

## Minimal `ci.yml` Example

```yaml
name: ci

on:
  pull_request:
  push:
    branches: [ main, develop ]

jobs:
  build-test-pack:
    runs-on: windows-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Restore
        run: dotnet restore src/ManicTimeMcp.slnx

      - name: Build
        run: dotnet build src/ManicTimeMcp.slnx -warnaserror

      - name: Test
        run: dotnet test --solution src/ManicTimeMcp.slnx

      - name: Pack
        run: dotnet pack src/ManicTimeMcp.slnx -c Release

      - name: Upload nupkg
        uses: actions/upload-artifact@v4
        with:
          name: nupkg
          path: src/**/bin/Release/*.nupkg
```

## Notes

- With .NET 10 + MTP, `dotnet test` must use `--solution` or `--project`.
- Keep warnings-as-errors enabled in `src/Directory.Build.props`.
- Coverage/TRX behavior should follow `MSTest.Sdk` defaults unless intentionally overridden.
