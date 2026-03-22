using AwesomeAssertions;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ResourceOperationsTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
	];

	private static readonly EnvironmentDto[] SampleEnvironments =
	[
		new() { EnvironmentId = 1, DeviceName = "TEST-PC" },
	];

	private static readonly TimelineSummaryDto[] SampleSummaries =
	[
		new() { ReportId = 1, StartLocalTime = "2025-01-01 00:00:00", EndLocalTime = "2025-01-31 23:59:59" },
	];

	private static ManicTimeResources CreateResources() =>
		new(
			new StubDataDirectoryResolver(@"C:\TestData"),
			new StubHealthService(),
			new StubTimelineRepository(SampleTimelines),
			new StubEnvironmentRepository(SampleEnvironments),
			new StubUsageRepository(summaries: SampleSummaries));

	[TestMethod]
	public void GetConfig_ReturnsDataDirectoryInfo()
	{
		var resources = CreateResources();

		var result = resources.GetConfig();

		result.Should().NotBeNull();
		result!["dataDirectory"]!.GetValue<string>().Should().Be(@"C:\TestData");
	}

	[TestMethod]
	public async Task GetTimelinesAsync_ReturnsTimelineArray()
	{
		var resources = CreateResources();

		var result = await resources.GetTimelinesAsync(CancellationToken.None).ConfigureAwait(false);

		result.Should().NotBeNull();
		result!.AsArray()[0]!["schemaName"]!.GetValue<string>().Should().Be("ManicTime/Applications");
	}

	[TestMethod]
	public async Task GetEnvironmentAsync_ReturnsEnvironmentData()
	{
		var resources = CreateResources();

		var result = await resources.GetEnvironmentAsync(CancellationToken.None).ConfigureAwait(false);

		result.Should().NotBeNull();
		result!["environments"]!.AsArray()[0]!["deviceName"]!.GetValue<string>().Should().Be("TEST-PC");
	}

	[TestMethod]
	public async Task GetDataRangeAsync_ReturnsTimelineSummaries()
	{
		var resources = CreateResources();

		var result = await resources.GetDataRangeAsync(CancellationToken.None).ConfigureAwait(false);

		result.Should().NotBeNull();
		result!["timelineSummaries"]!.AsArray().Count.Should().Be(1);
	}
}
