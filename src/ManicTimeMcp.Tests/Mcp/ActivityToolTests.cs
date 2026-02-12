using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ActivityToolTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
		new() { ReportId = 2, SchemaName = "ManicTime/ComputerUsage", BaseSchemaName = "ManicTime/ComputerUsage" },
		new() { ReportId = 3, SchemaName = "ManicTime/Tags", BaseSchemaName = "ManicTime/Tags" },
		new() { ReportId = 4, SchemaName = "ManicTime/Documents", BaseSchemaName = "ManicTime/Documents" },
	];

	private static readonly ActivityDto[] SampleActivities =
	[
		new() { ActivityId = 1, ReportId = 1, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00", Name = "VS Code", GroupId = null },
		new() { ActivityId = 2, ReportId = 1, StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 10:00:00", Name = "Chrome", GroupId = null },
		new() { ActivityId = 3, ReportId = 2, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 12:00:00", Name = "On", GroupId = null },
	];

	private static McpTestHarness CreateHarness()
	{
		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(SampleActivities));
			builder.WithTools<ActivityTools>();
		});
	}

	[TestMethod]
	public async Task ListTools_ContainsAllActivityTools()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var tools = await client.ListToolsAsync().ConfigureAwait(false);

		var toolNames = tools.Select(t => t.Name).ToList();
		toolNames.Should().Contain("get_activities");
		toolNames.Should().Contain("get_computer_usage");
		toolNames.Should().Contain("get_tags");
		toolNames.Should().Contain("get_application_usage");
		toolNames.Should().Contain("get_document_usage");
		toolNames.Should().Contain("get_daily_summary");
	}

	[TestMethod]
	public async Task GetActivities_ReturnsFilteredByTimeline()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activities",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["timelineId"] = 1L,
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
		doc.RootElement.GetProperty("timelineId").GetInt64().Should().Be(1);
	}

	[TestMethod]
	public async Task GetComputerUsage_ReturnsMatchingSchema()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_computer_usage",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("schemaName").GetString().Should().Be("ManicTime/ComputerUsage");
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(1);
	}

	[TestMethod]
	public async Task GetDailySummary_ReturnsSummaryForDate()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_daily_summary",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["date"] = "2025-01-15",
			}).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("date").GetString().Should().Be("2025-01-15");
		doc.RootElement.GetProperty("timelineSummaries").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
	}

	[TestMethod]
	public async Task GetApplicationUsage_ReturnsApplicationActivities()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_application_usage",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("schemaName").GetString().Should().Be("ManicTime/Applications");
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
	}
}
