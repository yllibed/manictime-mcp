using BenchmarkDotNet.Attributes;
using ManicTimeMcp.Database;

namespace ManicTimeMcp.Benchmarks;

/// <summary>Benchmarks for schema manifest lookups.</summary>
[MemoryDiagnoser]
public class SchemaValidationBenchmarks
{
	[Benchmark]
	public bool ManifestContainsKnownTable() => SchemaManifest.Tables.ContainsKey("Ar_Activity");

	[Benchmark]
	public bool ManifestContainsUnknownTable() => SchemaManifest.Tables.ContainsKey("NonExistentTable");

	[Benchmark]
	public int ManifestEnumerateAllTables()
	{
		var count = 0;
		foreach (var table in SchemaManifest.Tables.Values)
		{
			count += table.RequiredColumns.Count;
		}

		return count;
	}
}
