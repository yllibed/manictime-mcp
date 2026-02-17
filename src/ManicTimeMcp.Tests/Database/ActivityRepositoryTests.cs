using AwesomeAssertions;
using ManicTimeMcp.Database;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class ActivityRepositoryTests
{
	#region GetActivitiesAsync — basic queries

	[TestMethod]
	public async Task GetActivitiesAsync_FullDayRange_ReturnsAllForTimeline()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var activities = await sut.GetActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00").ConfigureAwait(false);

		// Timeline 2 (Applications) has 4 activities
		activities.Count.Should().Be(4);
	}

	[TestMethod]
	public async Task GetActivitiesAsync_NarrowRange_ReturnsOverlapping()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		// 09:00-10:30 overlaps with activity 3 (08:00-10:00) and activity 4 (10:00-11:30)
		var activities = await sut.GetActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 09:00:00",
			endLocalTime: "2025-01-15 10:30:00").ConfigureAwait(false);

		activities.Count.Should().Be(2);
	}

	[TestMethod]
	public async Task GetActivitiesAsync_NoOverlap_ReturnsEmpty()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var activities = await sut.GetActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-16 00:00:00",
			endLocalTime: "2025-01-17 00:00:00").ConfigureAwait(false);

		activities.Should().BeEmpty();
	}

	[TestMethod]
	public async Task GetActivitiesAsync_OrderedByStartLocalTime()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var activities = await sut.GetActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00").ConfigureAwait(false);

		for (var i = 1; i < activities.Count; i++)
		{
			string.Compare(activities[i].StartLocalTime, activities[i - 1].StartLocalTime, StringComparison.Ordinal)
				.Should().BeGreaterThanOrEqualTo(0);
		}
	}

	#endregion

	#region GetActivitiesAsync — null handling

	[TestMethod]
	public async Task GetActivitiesAsync_NullNameAndGroupId_MappedCorrectly()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		// Activity 8 on timeline 1 has null Name and GroupId (12:00-13:00)
		var activities = await sut.GetActivitiesAsync(
			timelineId: 1,
			startLocalTime: "2025-01-15 12:00:00",
			endLocalTime: "2025-01-15 12:59:00").ConfigureAwait(false);

		var nullActivity = activities.Should().ContainSingle().Which;
		nullActivity.Name.Should().BeNull();
		nullActivity.GroupId.Should().BeNull();
	}

	#endregion

	#region GetActivitiesAsync — limits

	[TestMethod]
	public async Task GetActivitiesAsync_ExplicitLimit_Respected()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var activities = await sut.GetActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00",
			limit: 2).ConfigureAwait(false);

		activities.Count.Should().Be(2);
	}

	[TestMethod]
	public async Task GetActivitiesAsync_LimitExceedingHardCap_ClampedToMax()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		// Requesting 10000 but hard cap is 5000; with only 4 rows, result is 4
		var activities = await sut.GetActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00",
			limit: 10000).ConfigureAwait(false);

		activities.Count.Should().Be(4);
	}

	#endregion

	#region GetActivitiesAsync — cancellation

	[TestMethod]
	public async Task GetActivitiesAsync_SupportsCancellation()
	{
		using var fixture = FixtureDatabase.CreateStandard();
		var sut = CreateRepository(fixture);
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync().ConfigureAwait(false);

		var act = () => sut.GetActivitiesAsync(
			timelineId: 1,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00",
			cancellationToken: cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
	}

	#endregion

	#region GetGroupsAsync

	[TestMethod]
	public async Task GetGroupsAsync_StandardData_ReturnsGroupsForTimeline()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var groups = await sut.GetGroupsAsync(timelineId: 2).ConfigureAwait(false);

		groups.Count.Should().Be(3);
		groups.Should().Contain(g => g.Name == "Visual Studio");
		groups.Should().Contain(g => g.Name == "Chrome");
		groups.Should().Contain(g => g.Name == "Terminal");
	}

	[TestMethod]
	public async Task GetGroupsAsync_DifferentTimeline_ReturnsOnlyMatching()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var groups = await sut.GetGroupsAsync(timelineId: 3).ConfigureAwait(false);

		groups.Should().ContainSingle().Which.Name.Should().Be("Project.sln");
	}

	[TestMethod]
	public async Task GetGroupsAsync_NoGroups_ReturnsEmpty()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var groups = await sut.GetGroupsAsync(timelineId: 1).ConfigureAwait(false);

		groups.Should().BeEmpty();
	}

	#endregion

	#region GetEnrichedActivitiesAsync — full variant

	[TestMethod]
	public async Task GetEnrichedActivitiesAsync_Full_ReturnsGroupAndCommonGroupData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetEnrichedActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00").ConfigureAwait(false);

		results.Count.Should().Be(4);
		var devenv = results.First(r => string.Equals(r.Name, "devenv.exe", StringComparison.Ordinal));
		devenv.GroupName.Should().Be("Visual Studio");
		devenv.GroupColor.Should().NotBeNull();
		devenv.GroupKey.Should().NotBeNull();
	}

	[TestMethod]
	public async Task GetEnrichedActivitiesAsync_Full_ReturnsTags()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetEnrichedActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00").ConfigureAwait(false);

		// Activity 3 (devenv.exe 08:00-10:00) has tag "coding"
		var devenvFirst = results.First(r =>
			string.Equals(r.Name, "devenv.exe", StringComparison.Ordinal)
			&& string.Equals(r.StartLocalTime, "2025-01-15 08:00:00", StringComparison.Ordinal));
		devenvFirst.Tags.Should().NotBeNull();
		devenvFirst.Tags.Should().Contain("coding");

		// Activity 4 (chrome.exe 10:00-11:30) has tag "browsing"
		var chrome = results.First(r => string.Equals(r.Name, "chrome.exe", StringComparison.Ordinal));
		chrome.Tags.Should().NotBeNull();
		chrome.Tags.Should().Contain("browsing");
	}

	[TestMethod]
	public async Task GetEnrichedActivitiesAsync_Full_NoTags_ReturnsNullTags()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture, fullCapabilities: true);

		var results = await sut.GetEnrichedActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00").ConfigureAwait(false);

		// Activity 5 (WindowsTerminal.exe 11:30-12:00) has no tags
		var terminal = results.First(r => string.Equals(r.Name, "WindowsTerminal.exe", StringComparison.Ordinal));
		terminal.Tags.Should().BeNull();
	}

	#endregion

	#region GetEnrichedActivitiesAsync — degraded variant

	[TestMethod]
	public async Task GetEnrichedActivitiesAsync_Degraded_ReturnsGroupDataOnly()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture, fullCapabilities: false);

		var results = await sut.GetEnrichedActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00").ConfigureAwait(false);

		results.Count.Should().Be(4);
		var devenv = results.First(r => string.Equals(r.Name, "devenv.exe", StringComparison.Ordinal));
		devenv.GroupName.Should().Be("Visual Studio");
		devenv.CommonGroupName.Should().BeNull();
		devenv.Tags.Should().BeNull();
	}

	[TestMethod]
	public async Task GetEnrichedActivitiesAsync_Degraded_LimitRespected()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture, fullCapabilities: false);

		var results = await sut.GetEnrichedActivitiesAsync(
			timelineId: 2,
			startLocalTime: "2025-01-15 00:00:00",
			endLocalTime: "2025-01-16 00:00:00",
			limit: 2).ConfigureAwait(false);

		results.Count.Should().Be(2);
	}

	#endregion

	#region Helpers

	private static ActivityRepository CreateRepository(FixtureDatabase fixture, bool fullCapabilities = false)
	{
		var factory = new FixtureConnectionFactory(fixture.FilePath);
		var capabilities = fullCapabilities
			? new QueryCapabilityMatrix(SchemaManifest.Tables.Values
				.Where(t => t.Tier != TableTier.Core)
				.Select(t => t.TableName))
			: new QueryCapabilityMatrix([]);
		return new ActivityRepository(factory, capabilities, NullLogger<ActivityRepository>.Instance);
	}

	#endregion
}
