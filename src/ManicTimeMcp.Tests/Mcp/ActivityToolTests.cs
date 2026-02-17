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

	private static readonly DailyUsageDto[] SampleDailyAppUsage =
	[
		new() { Day = "2025-01-15", Name = "VS Code", Color = "#007ACC", Key = "code.exe", TotalSeconds = 3600 },
		new() { Day = "2025-01-15", Name = "Chrome", Color = "#4285F4", Key = "chrome.exe", TotalSeconds = 1800 },
	];

	private static McpTestHarness CreateHarness(
		DailyUsageDto[]? dailyAppUsage = null,
		DailyUsageDto[]? dailyDocUsage = null)
	{
		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(SampleActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: dailyAppUsage ?? SampleDailyAppUsage,
				dailyDoc: dailyDocUsage));
			services.AddSingleton(CreateFullCapabilities());
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
	public async Task GetActivities_IncludesTruncationAndDiagnostics()
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

		var truncation = doc.RootElement.GetProperty("truncation");
		truncation.GetProperty("truncated").GetBoolean().Should().BeFalse();
		truncation.GetProperty("returnedCount").GetInt32().Should().Be(2);

		var diag = doc.RootElement.GetProperty("diagnostics");
		diag.GetProperty("degraded").GetBoolean().Should().BeFalse();
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

		// Verify truncation block present
		doc.RootElement.GetProperty("truncation").GetProperty("truncated").GetBoolean().Should().BeFalse();
	}

	[TestMethod]
	public async Task GetApplicationUsage_ReturnsDailyUsage()
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
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
		doc.RootElement.GetProperty("usage").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("diagnostics").GetProperty("degraded").GetBoolean().Should().BeFalse();
	}

	[TestMethod]
	public async Task GetActivities_InvalidDateFormat_ReturnsIsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activities",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["timelineId"] = 1L,
				["startDate"] = "not-a-date",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("Invalid date format");
	}

	/// <summary>Creates a capability matrix with all supplemental tables present.</summary>
	private static QueryCapabilityMatrix CreateFullCapabilities()
	{
		return new QueryCapabilityMatrix([
			"Ar_CommonGroup",
			"Ar_ApplicationByDay",
			"Ar_WebSiteByDay",
			"Ar_DocumentByDay",
			"Ar_ApplicationByYear",
			"Ar_WebSiteByYear",
			"Ar_DocumentByYear",
			"Ar_ActivityByHour",
			"Ar_TimelineSummary",
			"Ar_Environment",
			"Ar_Folder",
			"Ar_Tag",
			"Ar_ActivityTag",
			"Ar_Category",
			"Ar_CategoryGroup",
		]);
	}
}
