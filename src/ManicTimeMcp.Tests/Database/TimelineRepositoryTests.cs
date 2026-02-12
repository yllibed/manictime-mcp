using AwesomeAssertions;
using ManicTimeMcp.Database;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class TimelineRepositoryTests
{
	#region GetTimelinesAsync

	[TestMethod]
	public async Task GetTimelinesAsync_StandardData_ReturnsAllTimelines()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var timelines = await sut.GetTimelinesAsync().ConfigureAwait(false);

		timelines.Count.Should().Be(4);
		timelines[0].ReportId.Should().Be(1);
		timelines[0].SchemaName.Should().Be("ManicTime/ComputerUsage");
	}

	[TestMethod]
	public async Task GetTimelinesAsync_OrderedByReportId()
	{
		using var fixture = FixtureDatabase.CreateStandard(FixtureSeeder.SeedStandardData);
		var sut = CreateRepository(fixture);

		var timelines = await sut.GetTimelinesAsync().ConfigureAwait(false);

		for (var i = 1; i < timelines.Count; i++)
		{
			timelines[i].ReportId.Should().BeGreaterThan(timelines[i - 1].ReportId);
		}
	}

	[TestMethod]
	public async Task GetTimelinesAsync_EmptyDatabase_ReturnsEmptyList()
	{
		using var fixture = FixtureDatabase.CreateStandard(); // no seed
		var sut = CreateRepository(fixture);

		var timelines = await sut.GetTimelinesAsync().ConfigureAwait(false);

		timelines.Should().BeEmpty();
	}

	[TestMethod]
	public async Task GetTimelinesAsync_SupportsCancellation()
	{
		using var fixture = FixtureDatabase.CreateStandard();
		var sut = CreateRepository(fixture);
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync().ConfigureAwait(false);

		var act = () => sut.GetTimelinesAsync(cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
	}

	#endregion

	#region Helpers

	private static TimelineRepository CreateRepository(FixtureDatabase fixture)
	{
		var factory = new FixtureConnectionFactory(fixture.FilePath);
		return new TimelineRepository(factory, NullLogger<TimelineRepository>.Instance);
	}

	#endregion
}
