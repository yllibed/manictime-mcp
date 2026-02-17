using AwesomeAssertions;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Tests.Database;

/// <summary>Tests that the Database DI registration eagerly validates the schema.</summary>
[TestClass]
public sealed class ServiceCollectionExtensionsTests : IDisposable
{
	private readonly string _tempDir;

	public ServiceCollectionExtensionsTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), $"ManicTimeMcp_DI_{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDir);
	}

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, recursive: true);
			}
		}
#pragma warning disable CA1031 // Do not catch general exception types — cleanup best effort
		catch (Exception)
#pragma warning restore CA1031
		{
			// Best effort cleanup
		}
	}

	[TestMethod]
	public void QueryCapabilityMatrix_FullDb_PopulatedAtResolution()
	{
		using var fixture = FixtureDatabase.CreateFull();
		File.Copy(fixture.FilePath, Path.Combine(_tempDir, "ManicTimeReports.db"));

		using var sp = BuildServiceProvider(_tempDir);

		var matrix = sp.GetRequiredService<QueryCapabilityMatrix>();

		matrix.HasPreAggregatedAppUsage.Should().BeTrue();
		matrix.HasTimelineSummary.Should().BeTrue();
		matrix.HasEnvironment.Should().BeTrue();
		matrix.HasTags.Should().BeTrue();
	}

	[TestMethod]
	public void QueryCapabilityMatrix_CoreOnlyDb_DegradedAtResolution()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly();
		File.Copy(fixture.FilePath, Path.Combine(_tempDir, "ManicTimeReports.db"));

		using var sp = BuildServiceProvider(_tempDir);

		var matrix = sp.GetRequiredService<QueryCapabilityMatrix>();

		matrix.HasPreAggregatedAppUsage.Should().BeFalse();
		matrix.HasTimelineSummary.Should().BeFalse();
		matrix.HasTags.Should().BeFalse();
	}

	[TestMethod]
	public void QueryCapabilityMatrix_NullDataDir_FullyDegraded()
	{
		using var sp = BuildServiceProvider(dataDir: null);

		var matrix = sp.GetRequiredService<QueryCapabilityMatrix>();

		matrix.HasPreAggregatedAppUsage.Should().BeFalse();
		matrix.HasEnvironment.Should().BeFalse();
		matrix.GetDegradedCapabilities().Count.Should().BeGreaterThan(0);
	}

	[TestMethod]
	public void QueryCapabilityMatrix_MissingDbFile_FullyDegraded()
	{
		// _tempDir exists but has no ManicTimeReports.db file
		using var sp = BuildServiceProvider(_tempDir);

		var matrix = sp.GetRequiredService<QueryCapabilityMatrix>();

		matrix.HasPreAggregatedAppUsage.Should().BeFalse();
	}

	private static ServiceProvider BuildServiceProvider(string? dataDir)
	{
		var services = new ServiceCollection();
		services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
		services.AddSingleton<IDataDirectoryResolver>(new StubResolver(dataDir));
		services.AddManicTimeDatabase();
		return services.BuildServiceProvider();
	}

	private sealed class StubResolver(string? path) : IDataDirectoryResolver
	{
		public DataDirectoryResult Resolve() => new()
		{
			Path = path,
			Source = path is not null ? DataDirectorySource.EnvironmentVariable : DataDirectorySource.Unresolved,
		};
	}
}
