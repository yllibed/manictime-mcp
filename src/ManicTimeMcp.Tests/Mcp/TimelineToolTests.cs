using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class TimelineToolTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
		new() { ReportId = 2, SchemaName = "ManicTime/ComputerUsage", BaseSchemaName = "ManicTime/ComputerUsage" },
	];

	[TestMethod]
	public async Task ListTools_ContainsGetTimelines()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			builder.WithTools<TimelineTools>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var tools = await client.ListToolsAsync().ConfigureAwait(false);

		tools.Should().Contain(t => t.Name == "get_timelines");
	}

	[TestMethod]
	public async Task GetTimelines_ReturnsSerializedTimelines()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			builder.WithTools<TimelineTools>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_timelines",
			new Dictionary<string, object?>(StringComparer.Ordinal)).ConfigureAwait(false);

		result.Content.Should().ContainSingle();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
		var timelines = doc.RootElement.GetProperty("timelines");
		timelines.GetArrayLength().Should().Be(2);
		timelines[0].GetProperty("reportId").GetInt64().Should().Be(1);
		timelines[0].GetProperty("schemaName").GetString().Should().Be("ManicTime/Applications");

		// Verify truncation block
		doc.RootElement.GetProperty("truncation").GetProperty("truncated").GetBoolean().Should().BeFalse();
		doc.RootElement.GetProperty("diagnostics").GetProperty("degraded").GetBoolean().Should().BeFalse();
	}

	[TestMethod]
	public async Task GetTimelines_EmptyRepository_ReturnsEmptyArray()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository());
			builder.WithTools<TimelineTools>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_timelines",
			new Dictionary<string, object?>(StringComparer.Ordinal)).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(0);
		doc.RootElement.GetProperty("timelines").GetArrayLength().Should().Be(0);
	}
}
