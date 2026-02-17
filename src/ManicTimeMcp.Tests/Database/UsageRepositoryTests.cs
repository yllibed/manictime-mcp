using AwesomeAssertions;
using ManicTimeMcp.Database;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class UsageRepositoryTests
{
	[TestMethod]
	public async Task GetDailyAppUsageAsync_ReturnsSeededData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetDailyAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		results.Count.Should().Be(3);
		results.Should().Contain(r => r.Name == "Visual Studio" && r.TotalSeconds == 7200);
		results.Should().Contain(r => r.Name == "Chrome" && r.TotalSeconds == 5400);
	}

	[TestMethod]
	public async Task GetDailyAppUsageAsync_SpanningMultipleDays_ReturnsAll()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetDailyAppUsageAsync("2025-01-15", "2025-01-17").ConfigureAwait(false);

		results.Count.Should().Be(4);
	}

	[TestMethod]
	public async Task GetDailyWebUsageAsync_ReturnsSeededData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetDailyWebUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		results.Should().ContainSingle().Which.Name.Should().Be("Chrome");
	}

	[TestMethod]
	public async Task GetDailyDocUsageAsync_ReturnsSeededData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetDailyDocUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		results.Should().ContainSingle().Which.TotalSeconds.Should().Be(14400);
	}

	[TestMethod]
	public async Task GetHourlyAppUsageAsync_ReturnsSeededData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetHourlyAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		results.Count.Should().Be(3);
		results.Should().Contain(r => r.Hour == 8 && r.Name == "Visual Studio");
		results.Should().Contain(r => r.Hour == 10 && r.Name == "Chrome");
	}

	[TestMethod]
	public async Task GetHourlyAppUsageAsync_IncludesColorAndKey()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetHourlyAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		var vs = results.First(r => string.Equals(r.Name, "Visual Studio", StringComparison.Ordinal));
		vs.Color.Should().Be("#FF0000");
		vs.Key.Should().Be("devenv.exe");
	}

	[TestMethod]
	public async Task GetTimelineSummariesAsync_ReturnsSeededData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetTimelineSummariesAsync().ConfigureAwait(false);

		results.Count.Should().Be(2);
		results.Should().Contain(r => r.ReportId == 1);
		results.Should().Contain(r => r.ReportId == 2);
	}

	[TestMethod]
	public async Task GetDailyAppUsageAsync_NoDataInRange_ReturnsEmpty()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetDailyAppUsageAsync("2025-02-01", "2025-02-02").ConfigureAwait(false);

		results.Should().BeEmpty();
	}

	[TestMethod]
	public async Task GetDailyAppUsageAsync_LimitRespected()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetDailyAppUsageAsync("2025-01-15", "2025-01-16", limit: 1).ConfigureAwait(false);

		results.Count.Should().Be(1);
	}

	[TestMethod]
	public async Task GetDayOfWeekAppUsageAsync_ReturnsAggregatedData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetDayOfWeekAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		// Seeded Ar_ApplicationByYear has one entry for 2025-01-15 (Wednesday = 3)
		results.Should().ContainSingle();
		results[0].Name.Should().Be("Visual Studio");
		results[0].DayOfWeek.Should().Be(3); // Wednesday
		results[0].TotalSeconds.Should().Be(7200);
	}

	private static UsageRepository CreateRepository(FixtureDatabase fixture, bool fullCapabilities = true)
	{
		var factory = new FixtureConnectionFactory(fixture.FilePath);
		var capabilities = fullCapabilities
			? new QueryCapabilityMatrix(SchemaManifest.Tables.Values
				.Where(t => t.Tier != TableTier.Core)
				.Select(t => t.TableName))
			: new QueryCapabilityMatrix([]);
		return new UsageRepository(factory, capabilities, NullLogger<UsageRepository>.Instance);
	}
}
