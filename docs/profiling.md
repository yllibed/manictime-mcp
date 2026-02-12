# Profiling and Performance Operations

## Overview

This document describes the profiling infrastructure for ManicTime MCP.
Performance measurement is automated through BenchmarkDotNet and can be
executed by both humans and autonomous agents.

## Running Benchmarks

```powershell
# Run all benchmarks
dotnet run -c Release --project src/ManicTimeMcp.Benchmarks/ManicTimeMcp.Benchmarks.csproj

# Run a specific benchmark class
dotnet run -c Release --project src/ManicTimeMcp.Benchmarks/ManicTimeMcp.Benchmarks.csproj -- --filter "*ScreenshotParser*"

# Quick smoke run (fewer iterations)
dotnet run -c Release --project src/ManicTimeMcp.Benchmarks/ManicTimeMcp.Benchmarks.csproj -- --job short
```

## Benchmark Scenarios

| Benchmark | Category | What it measures |
|-----------|----------|-----------------|
| `ScreenshotParserBenchmarks` | Hot path | ReadOnlySpan-based filename parsing (canonical, thumbnail, malformed) |
| `QueryLimitsBenchmarks` | Core logic | Clamp computation overhead |
| `SchemaValidationBenchmarks` | Startup | FrozenDictionary lookups in schema manifest |

## Metrics Tracked

All benchmarks use `[MemoryDiagnoser]` to capture:
- **Mean execution time** (ns)
- **Allocated bytes** per operation
- **GC collections** per 1000 operations

## CI Integration

- **PR CI**: No benchmarks run (too slow for feedback loop).
- **Nightly CI** (future): Run benchmarks and compare against baseline.
- **Release CI** (future): Gate on regression thresholds when baselines are established.

## Baseline Policy

- Baselines are stored as BenchmarkDotNet result files.
- Re-baseline after major runtime, SDK, or dependency upgrades.
- Regression thresholds will be defined once initial baselines are established.

## Autonomous Agent Usage

Agents can execute benchmarks non-interactively:

```bash
dotnet run -c Release --project src/ManicTimeMcp.Benchmarks/ManicTimeMcp.Benchmarks.csproj -- --exporters json
```

The `--exporters json` flag produces machine-readable output for automated comparison.
