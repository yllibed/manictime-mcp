using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class NarrativeToolTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
	];

	private static readonly ActivityDto[] SampleActivities =
	[
		new() { ActivityId = 1, ReportId = 1, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00", Name = "VS Code", GroupId = null },
		new() { ActivityId = 2, ReportId = 1, StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 10:00:00", Name = "Chrome", GroupId = null },
	];

	private static readonly DailyUsageDto[] SampleDailyAppUsage =
	[
		new() { Day = "2025-01-15", Name = "VS Code", Color = "#007ACC", Key = "code.exe", TotalSeconds = 3600 },
		new() { Day = "2025-01-15", Name = "Chrome", Color = "#4285F4", Key = "chrome.exe", TotalSeconds = 1800 },
	];

	private static readonly EnrichedActivityDto[] SampleEnrichedActivities =
	[
		new()
		{
			ActivityId = 1, ReportId = 1,
			StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00",
			Name = "VS Code", GroupId = null, GroupColor = "#007ACC",
			CommonGroupName = "Visual Studio Code", Tags = ["coding", "work"],
		},
		new()
		{
			ActivityId = 2, ReportId = 1,
			StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 10:00:00",
			Name = "Chrome", GroupId = null, GroupColor = "#4285F4",
			CommonGroupName = "Google Chrome",
		},
	];

	private static readonly HourlyUsageDto[] SampleHourlyAppUsage =
	[
		new() { Day = "2025-01-15", Hour = 8, Name = "VS Code", Color = "#007ACC", Key = "code.exe", TotalSeconds = 3600 },
		new() { Day = "2025-01-15", Hour = 17, Name = "Chrome", Color = "#4285F4", Key = "chrome.exe", TotalSeconds = 1800 },
	];

	private static readonly DailyUsageDto[] SampleDailyWebUsage =
	[
		new() { Day = "2025-01-15", Name = "github.com", TotalSeconds = 1200 },
		new() { Day = "2025-01-15", Name = "stackoverflow.com", TotalSeconds = 600 },
	];

	private static McpTestHarness CreateHarness()
	{
		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(SampleActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: SampleDailyAppUsage,
				dailyWeb: SampleDailyWebUsage));
			services.AddSingleton(CreateFullCapabilities());
			builder.WithTools<NarrativeTools>();
		});
	}

	[TestMethod]
	public async Task ListTools_ContainsNarrativeTools()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var tools = await client.ListToolsAsync().ConfigureAwait(false);

		var toolNames = tools.Select(t => t.Name).ToList();
		toolNames.Should().Contain("get_activity_narrative");
		toolNames.Should().Contain("get_period_summary");
		toolNames.Should().Contain("get_website_usage");
		toolNames.Should().Contain("get_daily_summary");
	}

	[TestMethod]
	public async Task GetActivityNarrative_ReturnsSegmentsAndTopApps()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activity_narrative",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		doc.RootElement.GetProperty("startDate").GetString().Should().Be("2025-01-15");
		doc.RootElement.GetProperty("segments").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("topApplications").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("truncation").GetProperty("truncated").GetBoolean().Should().BeFalse();
		doc.RootElement.GetProperty("diagnostics").GetProperty("degraded").GetBoolean().Should().BeFalse();
	}

	[TestMethod]
	public async Task GetActivityNarrative_WithWebsites_IncludesTopWebsites()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activity_narrative",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
				["includeWebsites"] = true,
			}).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("topWebsites").GetArrayLength().Should().Be(2);
	}

	[TestMethod]
	public async Task GetPeriodSummary_ReturnsDaysAndAggregate()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_period_summary",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		doc.RootElement.GetProperty("days").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
		doc.RootElement.GetProperty("aggregate").GetProperty("topApps").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
		doc.RootElement.GetProperty("truncation").GetProperty("truncated").GetBoolean().Should().BeFalse();
	}

	[TestMethod]
	public async Task GetPeriodSummary_IncludesFirstAndLastActivity()
	{
		await using var harness = CreateHarnessWithHourlyData();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_period_summary",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		var days = doc.RootElement.GetProperty("days");
		days.GetArrayLength().Should().Be(1);
		var day = days[0];
		day.GetProperty("firstActivity").GetString().Should().Be("VS Code");
		day.GetProperty("lastActivity").GetString().Should().Be("Chrome");
	}

	[TestMethod]
	public async Task GetPeriodSummary_ExceedsMaxDays_ReturnsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_period_summary",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-01",
				["endDate"] = "2025-03-01",
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("maximum");
	}

	[TestMethod]
	public async Task GetWebsiteUsage_ShortRange_ReturnsHourlyGranularity()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_website_usage",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("breakdownGranularity").GetString().Should().Be("hour");
	}

	[TestMethod]
	public async Task GetWebsiteUsage_LongRange_ReturnsDailyGranularity()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_website_usage",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-01",
				["endDate"] = "2025-01-15",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("breakdownGranularity").GetString().Should().Be("day");
	}

	[TestMethod]
	public async Task GetActivityNarrative_IncludesTagsAndRefs()
	{
		await using var harness = CreateEnrichedHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activity_narrative",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		var segments = doc.RootElement.GetProperty("segments");
		segments.GetArrayLength().Should().Be(2);

		// First segment has tags
		var first = segments[0];
		first.GetProperty("tags").GetArrayLength().Should().Be(2);
		first.GetProperty("tags")[0].GetString().Should().Be("coding");

		// Second segment has no tags (null)
		var second = segments[1];
		second.GetProperty("tags").ValueKind.Should().Be(JsonValueKind.Null);

		// Both segments have refs
		first.GetProperty("refs").GetProperty("timelineRef").GetInt64().Should().Be(1);
		first.GetProperty("refs").GetProperty("activityRef").GetInt64().Should().Be(1);
		second.GetProperty("refs").GetProperty("activityRef").GetInt64().Should().Be(2);
	}

	[TestMethod]
	public async Task GetDailySummary_DelegatesToNarrativeLogic()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_daily_summary",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["date"] = "2025-01-15",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		// Verify narrative response shape (not legacy timelineSummaries)
		doc.RootElement.GetProperty("startDate").GetString().Should().Be("2025-01-15");
		doc.RootElement.GetProperty("endDate").GetString().Should().Be("2025-01-16");
		doc.RootElement.GetProperty("segments").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("topApplications").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("truncation").GetProperty("truncated").GetBoolean().Should().BeFalse();
		doc.RootElement.GetProperty("diagnostics").GetProperty("degraded").GetBoolean().Should().BeFalse();
	}

	[TestMethod]
	public async Task GetDailySummary_InvalidDate_ReturnsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_daily_summary",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["date"] = "not-a-date",
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("Invalid date format");
	}

	[TestMethod]
	public async Task GetActivityNarrative_InvalidDate_ReturnsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activity_narrative",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "bad",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
	}

	private static McpTestHarness CreateHarnessWithHourlyData()
	{
		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(SampleActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: SampleDailyAppUsage,
				dailyWeb: SampleDailyWebUsage,
				hourlyApp: SampleHourlyAppUsage));
			services.AddSingleton(CreateFullCapabilities());
			builder.WithTools<NarrativeTools>();
		});
	}

	private static McpTestHarness CreateEnrichedHarness()
	{
		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(
				activities: SampleActivities,
				enrichedActivities: SampleEnrichedActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: SampleDailyAppUsage,
				dailyWeb: SampleDailyWebUsage));
			services.AddSingleton(CreateFullCapabilities());
			builder.WithTools<NarrativeTools>();
		});
	}

	private static readonly EnrichedActivityDto[] ConsecutiveSameAppActivities =
	[
		new()
		{
			ActivityId = 1, ReportId = 1,
			StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00",
			Name = "VS Code", GroupId = null, GroupColor = "#007ACC",
			CommonGroupName = "Visual Studio Code", Tags = ["coding"],
		},
		new()
		{
			ActivityId = 2, ReportId = 1,
			StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 10:00:00",
			Name = "VS Code", GroupId = null, GroupColor = "#007ACC",
			CommonGroupName = "Visual Studio Code", Tags = ["work"],
		},
		new()
		{
			ActivityId = 3, ReportId = 1,
			StartLocalTime = "2025-01-15 10:00:00", EndLocalTime = "2025-01-15 11:00:00",
			Name = "Chrome", GroupId = null, GroupColor = "#4285F4",
			CommonGroupName = "Google Chrome",
		},
	];

	[TestMethod]
	public async Task GetActivityNarrative_MergesConsecutiveSameAppSegments()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(
				enrichedActivities: ConsecutiveSameAppActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: SampleDailyAppUsage,
				dailyWeb: SampleDailyWebUsage));
			services.AddSingleton(CreateFullCapabilities());
			builder.WithTools<NarrativeTools>();
		});
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activity_narrative",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		// Two VS Code segments merged into one + one Chrome = 2 total segments
		var segments = doc.RootElement.GetProperty("segments");
		segments.GetArrayLength().Should().Be(2);

		// First merged segment should span 08:00 - 10:00 (120 min)
		var first = segments[0];
		first.GetProperty("start").GetString().Should().Be("2025-01-15 08:00:00");
		first.GetProperty("end").GetString().Should().Be("2025-01-15 10:00:00");
		first.GetProperty("durationMinutes").GetDouble().Should().Be(120.0);

		// Tags should be merged (union)
		var tags = first.GetProperty("tags");
		tags.GetArrayLength().Should().Be(2);
	}

	[TestMethod]
	public async Task GetActivityNarrative_TotalMinutesReflectsAllActivities()
	{
		var manyActivities = GenerateAlternatingActivities(count: 250, minutesEach: 2);

		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(
				enrichedActivities: manyActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: SampleDailyAppUsage,
				dailyWeb: SampleDailyWebUsage));
			services.AddSingleton(CreateFullCapabilities());
			builder.WithTools<NarrativeTools>();
		});
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_activity_narrative",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		// totalActiveMinutes should reflect ALL 250 activities (500 min), not just truncated subset
		var totalMinutes = doc.RootElement.GetProperty("totalActiveMinutes").GetDouble();
		totalMinutes.Should().Be(500.0);
	}

	/// <summary>Generates activities alternating between 50 apps so consecutive merging is minimal.</summary>
	private static EnrichedActivityDto[] GenerateAlternatingActivities(int count, int minutesEach)
	{
		var activities = new EnrichedActivityDto[count];
		var baseTime = new DateTime(2025, 1, 15, 6, 0, 0);
		for (var i = 0; i < count; i++)
		{
			var start = baseTime.AddMinutes(i * minutesEach);
			var end = start.AddMinutes(minutesEach);
			activities[i] = new EnrichedActivityDto
			{
				ActivityId = i + 1,
				ReportId = 1,
				StartLocalTime = start.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
				EndLocalTime = end.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
				Name = $"App{i % 50}",
				GroupId = null,
				CommonGroupName = $"App{i % 50}",
			};
		}

		return activities;
	}

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
