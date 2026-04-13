using AwesomeAssertions;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Models;

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

		result.DataDirectory.Should().Be(@"C:\TestData");
	}

	[TestMethod]
	public async Task GetTimelinesAsync_ReturnsTimelineList()
	{
		var resources = CreateResources();

		var result = await resources.GetTimelinesAsync(CancellationToken.None).ConfigureAwait(false);

		result.Should().ContainSingle();
		result[0].SchemaName.Should().Be("ManicTime/Applications");
	}

	[TestMethod]
	public void GetHealth_ReturnsHealthReport()
	{
		var resources = CreateResources();

		var result = resources.GetHealth();

		result.Should().NotBeNull();
		result.Status.Should().Be(HealthStatus.Healthy);
	}

	[TestMethod]
	public async Task GetEnvironmentAsync_ReturnsEnvironmentData()
	{
		var resources = CreateResources();

		var result = await resources.GetEnvironmentAsync(CancellationToken.None).ConfigureAwait(false);

		result.Environments.Should().ContainSingle();
		result.Environments[0].DeviceName.Should().Be("TEST-PC");
	}

	[TestMethod]
	public async Task GetDataRangeAsync_ReturnsTimelineSummaries()
	{
		var resources = CreateResources();

		var result = await resources.GetDataRangeAsync(CancellationToken.None).ConfigureAwait(false);

		result.TimelineSummaries.Should().ContainSingle();
	}

	[TestMethod]
	public void GetGuide_UsesReplFirstCommandsAndResourceUris()
	{
		var guide = ManicTimeResources.GetGuide();

		guide.Should().Contain("timeline list");
		guide.Should().Contain("summary narrative");
		guide.Should().Contain("screenshot save");
		guide.Should().Contain("manictime://resource/health");
		guide.Should().Contain("manictime://resource/data-range");
		guide.Should().NotContain("get_timelines");
		guide.Should().NotContain("manictime://health");
	}
}
