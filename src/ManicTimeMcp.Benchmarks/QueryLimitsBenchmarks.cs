using BenchmarkDotNet.Attributes;
using ManicTimeMcp.Database;

namespace ManicTimeMcp.Benchmarks;

/// <summary>Benchmarks for QueryLimits clamp operations.</summary>
[MemoryDiagnoser]
public class QueryLimitsBenchmarks
{
	[Benchmark]
	public int ClampWithDefault() => QueryLimits.Clamp(requested: null, defaultLimit: 1000, hardCap: 5000);

	[Benchmark]
	public int ClampWithinBounds() => QueryLimits.Clamp(requested: 500, defaultLimit: 1000, hardCap: 5000);

	[Benchmark]
	public int ClampAboveCap() => QueryLimits.Clamp(requested: 10000, defaultLimit: 1000, hardCap: 5000);
}
