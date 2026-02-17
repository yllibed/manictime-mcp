using AwesomeAssertions;
using ManicTimeMcp.Database;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class CorrelationRepositoryTests
{
	[TestMethod]
	public async Task GetCorrelatedActivitiesAsync_Full_ReturnsAllTimelines()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetCorrelatedActivitiesAsync(
			"2025-01-15 00:00:00", "2025-01-16 00:00:00").ConfigureAwait(false);

		// Activities across all timelines (timeline 1, 2, 3) that overlap the date
		results.Count.Should().BeGreaterThan(0);

		// Should include activities from different schemas
		results.Should().Contain(r => string.Equals(r.SchemaName, "ManicTime/Applications", StringComparison.Ordinal));
		results.Should().Contain(r => string.Equals(r.SchemaName, "ManicTime/ComputerUsage", StringComparison.Ordinal));
	}

	[TestMethod]
	public async Task GetCorrelatedActivitiesAsync_Full_IncludesCommonGroupName()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetCorrelatedActivitiesAsync(
			"2025-01-15 00:00:00", "2025-01-16 00:00:00").ConfigureAwait(false);

		var devenv = results.First(r => string.Equals(r.Name, "devenv.exe", StringComparison.Ordinal));
		devenv.CommonGroupName.Should().Be("Visual Studio");
		devenv.GroupName.Should().Be("Visual Studio");
	}

	[TestMethod]
	public async Task GetCorrelatedActivitiesAsync_Degraded_OmitsCommonGroup()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture, fullCapabilities: false);

		var results = await sut.GetCorrelatedActivitiesAsync(
			"2025-01-15 00:00:00", "2025-01-16 00:00:00").ConfigureAwait(false);

		results.Count.Should().BeGreaterThan(0);
		var devenv = results.First(r => string.Equals(r.Name, "devenv.exe", StringComparison.Ordinal));
		devenv.CommonGroupName.Should().BeNull();
		devenv.GroupName.Should().Be("Visual Studio");
	}

	[TestMethod]
	public async Task GetCorrelatedActivitiesAsync_OrderedByStartTimeThenSchema()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetCorrelatedActivitiesAsync(
			"2025-01-15 00:00:00", "2025-01-16 00:00:00").ConfigureAwait(false);

		for (var i = 1; i < results.Count; i++)
		{
			var cmp = string.Compare(results[i].StartLocalTime, results[i - 1].StartLocalTime, StringComparison.Ordinal);
			if (cmp == 0)
			{
				string.Compare(results[i].SchemaName, results[i - 1].SchemaName, StringComparison.Ordinal)
					.Should().BeGreaterThanOrEqualTo(0);
			}
			else
			{
				cmp.Should().BeGreaterThanOrEqualTo(0);
			}
		}
	}

	[TestMethod]
	public async Task GetCorrelatedActivitiesAsync_LimitRespected()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetCorrelatedActivitiesAsync(
			"2025-01-15 00:00:00", "2025-01-16 00:00:00", limit: 2).ConfigureAwait(false);

		results.Count.Should().Be(2);
	}

	[TestMethod]
	public async Task GetCorrelatedActivitiesAsync_NoOverlap_ReturnsEmpty()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetCorrelatedActivitiesAsync(
			"2025-02-01 00:00:00", "2025-02-02 00:00:00").ConfigureAwait(false);

		results.Should().BeEmpty();
	}

	private static CorrelationRepository CreateRepository(FixtureDatabase fixture, bool fullCapabilities)
	{
		var factory = new FixtureConnectionFactory(fixture.FilePath);
		var capabilities = fullCapabilities
			? new QueryCapabilityMatrix(SchemaManifest.Tables.Values
				.Where(t => t.Tier != TableTier.Core)
				.Select(t => t.TableName))
			: new QueryCapabilityMatrix([]);
		return new CorrelationRepository(factory, capabilities, NullLogger<CorrelationRepository>.Instance);
	}
}
