using AwesomeAssertions;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class TimelineToolsTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
		new() { ReportId = 2, SchemaName = "ManicTime/ComputerUsage", BaseSchemaName = "ManicTime/ComputerUsage" },
	];

	[TestMethod]
	public async Task GetTimelinesAsync_ReturnsSerializedTimelines()
	{
		var tools = new TimelineTools(new StubTimelineRepository(SampleTimelines));

		var result = await tools.GetTimelinesAsync(CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
		doc.RootElement.GetProperty("timelines").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("diagnostics").GetProperty("degraded").GetBoolean().Should().BeFalse();
	}

	[TestMethod]
	public async Task GetTimelinesAsync_EmptyRepository_ReturnsEmptyPayload()
	{
		var tools = new TimelineTools(new StubTimelineRepository());

		var result = await tools.GetTimelinesAsync(CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(0);
		doc.RootElement.GetProperty("timelines").GetArrayLength().Should().Be(0);
	}
}
