using AwesomeAssertions;
using ManicTimeMcp.Database;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Database;

/// <summary>Tests fallback query paths when pre-aggregated tables are absent.</summary>
[TestClass]
public sealed class UsageRepositoryFallbackTests
{
	[TestMethod]
	public async Task GetDailyAppUsageAsync_Fallback_ComputesFromActivity()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedStandardData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetDailyAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		// Seeded application activities: devenv 08-10 (2h), chrome 10-11:30 (1.5h),
		// terminal 11:30-12 (0.5h), devenv 13-17:30 (4.5h)
		results.Count.Should().BeGreaterThan(0);
		results.Should().Contain(r => string.Equals(r.Name, "Visual Studio", StringComparison.Ordinal));
	}

	[TestMethod]
	public async Task GetHourlyAppUsageAsync_Fallback_ComputesFromActivity()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedStandardData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetHourlyAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		results.Count.Should().BeGreaterThan(0);
		results.Should().Contain(r => r.Hour == 8);
	}

	[TestMethod]
	public async Task GetTimelineSummariesAsync_Fallback_ComputesFromActivity()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedStandardData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetTimelineSummariesAsync().ConfigureAwait(false);

		// Should have entries for each timeline that has activities
		results.Count.Should().BeGreaterThan(0);
		results.Should().Contain(r => r.ReportId == 1); // ComputerUsage
		results.Should().Contain(r => r.ReportId == 2); // Applications
	}

	[TestMethod]
	public async Task GetDayOfWeekAppUsageAsync_Fallback_ComputesFromActivity()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedStandardData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetDayOfWeekAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		// 2025-01-15 is a Wednesday (strftime('%w') = 3)
		results.Count.Should().BeGreaterThan(0);
		results.Should().OnlyContain(r => r.DayOfWeek == 3);
	}

	[TestMethod]
	public async Task GetDailyDocUsageAsync_Fallback_ComputesFromActivity()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedStandardData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetDailyDocUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		// Documents timeline has activity 7: Program.cs 08:00-12:00 (4h)
		results.Count.Should().BeGreaterThan(0);
	}

	[TestMethod]
	public async Task GetDailyAppUsageAsync_Fallback_NoDataInRange_ReturnsEmpty()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedStandardData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetDailyAppUsageAsync("2025-02-01", "2025-02-02").ConfigureAwait(false);

		results.Should().BeEmpty();
	}

	[TestMethod]
	public async Task GetDailyAppUsageAsync_Fallback_LimitRespected()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedStandardData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetDailyAppUsageAsync("2025-01-15", "2025-01-16", limit: 1).ConfigureAwait(false);

		results.Count.Should().Be(1);
	}

	[TestMethod]
	public async Task GetDailyAppUsageAsync_Fallback_SplitsAtMidnight()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedCrossMidnightData);
		var sut = CreateDegradedRepository(fixture);

		// Query both days
		var results = await sut.GetDailyAppUsageAsync("2025-01-15", "2025-01-17").ConfigureAwait(false);

		// The cross-midnight activity (23:30-00:30) should split: 30 min to 2025-01-15, 30 min to 2025-01-16
		var day15 = results.Where(r => string.Equals(r.Day, "2025-01-15", StringComparison.Ordinal)).ToList();
		var day16 = results.Where(r => string.Equals(r.Day, "2025-01-16", StringComparison.Ordinal)).ToList();

		day15.Should().NotBeEmpty("activity starts on 2025-01-15");
		day16.Should().NotBeEmpty("activity crosses midnight into 2025-01-16");

		// Day 15 total: 30 min (midnight portion) + 30 min (08:45-09:15) = 60 min
		var day15Seconds = day15.Sum(r => r.TotalSeconds);
		day15Seconds.Should().BeApproximately(3600, 1.0, "30 min before midnight + 30 min hour-boundary activity");

		// Day 16 total: 30 min (post-midnight)
		var day16Seconds = day16.Sum(r => r.TotalSeconds);
		day16Seconds.Should().BeApproximately(1800, 1.0, "30 min after midnight");
	}

	[TestMethod]
	public async Task GetHourlyAppUsageAsync_Fallback_SplitsAtHourBoundary()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly(FixtureSeeder.SeedCrossMidnightData);
		var sut = CreateDegradedRepository(fixture);

		var results = await sut.GetHourlyAppUsageAsync("2025-01-15", "2025-01-16").ConfigureAwait(false);

		// Activity 08:45-09:15 should split into hour 8 (15 min) and hour 9 (15 min)
		var hour8 = results.Where(r => string.Equals(r.Day, "2025-01-15", StringComparison.Ordinal) && r.Hour == 8).ToList();
		var hour9 = results.Where(r => string.Equals(r.Day, "2025-01-15", StringComparison.Ordinal) && r.Hour == 9).ToList();

		hour8.Should().NotBeEmpty("activity starts in hour 8");
		hour9.Should().NotBeEmpty("activity extends into hour 9");

		hour8.Sum(r => r.TotalSeconds).Should().BeApproximately(900, 1.0, "15 min in hour 8");
		hour9.Sum(r => r.TotalSeconds).Should().BeApproximately(900, 1.0, "15 min in hour 9");
	}

	private static UsageRepository CreateDegradedRepository(FixtureDatabase fixture)
	{
		var factory = new FixtureConnectionFactory(fixture.FilePath);
		var capabilities = new QueryCapabilityMatrix([]);
		return new UsageRepository(factory, capabilities, NullLogger<UsageRepository>.Instance);
	}
}
