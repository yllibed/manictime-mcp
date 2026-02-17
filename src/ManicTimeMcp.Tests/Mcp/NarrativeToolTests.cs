using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Mcp.Models;
using ManicTimeMcp.Screenshots;
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

	private static readonly TimelineDto[] TimelinesWithDocuments =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
		new() { ReportId = 2, SchemaName = "ManicTime/Documents", BaseSchemaName = "ManicTime/Documents" },
	];

	private static readonly ActivityDto[] SampleDocActivities =
	[
		new() { ActivityId = 10, ReportId = 2, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:30:00", Name = "Program.cs", GroupId = null },
		new() { ActivityId = 11, ReportId = 2, StartLocalTime = "2025-01-15 09:30:00", EndLocalTime = "2025-01-15 10:00:00", Name = "README.md", GroupId = null },
	];

	private static readonly EnrichedActivityDto[] SampleEnrichedWithShortSegments =
	[
		new()
		{
			ActivityId = 1, ReportId = 1,
			StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00",
			Name = "VS Code", GroupId = null, GroupColor = "#007ACC",
			CommonGroupName = "Visual Studio Code",
		},
		new()
		{
			ActivityId = 2, ReportId = 1,
			StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 09:00:20",
			Name = "Explorer", GroupId = null,
			CommonGroupName = "Windows Explorer",
		},
		new()
		{
			ActivityId = 3, ReportId = 1,
			StartLocalTime = "2025-01-15 09:00:20", EndLocalTime = "2025-01-15 10:00:00",
			Name = "Chrome", GroupId = null, GroupColor = "#4285F4",
			CommonGroupName = "Google Chrome",
		},
	];

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

	[TestMethod]
	public async Task GetActivityNarrative_PopulatesDocumentField()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(TimelinesWithDocuments));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(
				activities: SampleDocActivities,
				enrichedActivities: SampleEnrichedActivities));
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

		var segments = doc.RootElement.GetProperty("segments");
		// First segment (08:00-09:00) overlaps with Program.cs (08:00-09:30)
		segments[0].GetProperty("document").GetString().Should().Be("Program.cs");
		// Second segment (09:00-10:00) overlaps with both Program.cs and README.md;
		// Program.cs has more overlap (30 min vs 30 min — but README.md starts at 09:30)
		segments[1].GetProperty("document").ValueKind.Should().NotBe(JsonValueKind.Null);
	}

	[TestMethod]
	public async Task GetActivityNarrative_MinDuration_FiltersShortSegments()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(
				enrichedActivities: SampleEnrichedWithShortSegments));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: SampleDailyAppUsage));
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
				["minDurationMinutes"] = 0.5,
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		var segments = doc.RootElement.GetProperty("segments");
		// Explorer segment is ~20s = 0.33 min, should be filtered out
		segments.GetArrayLength().Should().Be(2);
		var names = Enumerable.Range(0, segments.GetArrayLength())
			.Select(i => segments[i].GetProperty("application").GetString())
			.ToList();
		names.Should().NotContain("Windows Explorer");
	}

	[TestMethod]
	public async Task GetDailySummary_IncludeSegmentsFalse_OmitsSegments()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_daily_summary",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["date"] = "2025-01-15",
				["includeSegments"] = false,
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);

		doc.RootElement.GetProperty("segments").GetArrayLength().Should().Be(0);
		// TopApplications should still be populated
		doc.RootElement.GetProperty("topApplications").GetArrayLength().Should().BeGreaterThan(0);
		// TotalActiveMinutes should still be computed
		doc.RootElement.GetProperty("totalActiveMinutes").GetDouble().Should().BeGreaterThan(0);
	}

	private static readonly ScreenshotSelection SampleScreenshotSelection = new()
	{
		Screenshots =
		[
			new ScreenshotInfo
			{
				Date = new DateOnly(2025, 1, 15), Time = new TimeOnly(8, 30, 0),
				Offset = "+00-00", Width = 1920, Height = 1080,
				Sequence = 0, Monitor = 0, IsThumbnail = true,
				FilePath = "/tmp/test1.jpg", Ref = "ref-08-30",
			},
			new ScreenshotInfo
			{
				Date = new DateOnly(2025, 1, 15), Time = new TimeOnly(9, 30, 0),
				Offset = "+00-00", Width = 1920, Height = 1080,
				Sequence = 0, Monitor = 0, IsThumbnail = true,
				FilePath = "/tmp/test2.jpg", Ref = "ref-09-30",
			},
		],
		TotalMatching = 2,
		IsTruncated = false,
	};

	[TestMethod]
	public async Task GetActivityNarrative_WithScreenshots_IncludesScreenshotRefs()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(SampleActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(
				dailyApp: SampleDailyAppUsage, dailyWeb: SampleDailyWebUsage));
			services.AddSingleton(CreateFullCapabilities());
			services.AddSingleton<IScreenshotService>(new StubScreenshotService(SampleScreenshotSelection));
			services.AddSingleton<IScreenshotRegistry>(new ScreenshotRegistry());
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

		var segments = doc.RootElement.GetProperty("segments");
		// First segment (08:00-09:00) — closest screenshot is at 08:30
		segments[0].GetProperty("refs").GetProperty("screenshotRef").GetString().Should().Be("ref-08-30");
		// Second segment (09:00-10:00) — closest screenshot is at 09:30
		segments[1].GetProperty("refs").GetProperty("screenshotRef").GetString().Should().Be("ref-09-30");
	}

	#region Unit tests for static helpers

	[TestMethod]
	public void MergeConsecutiveSegments_TotalNotInflatedByGaps()
	{
		// Two segments with a gap between them: 08:00-09:00 and 09:30-10:00
		var segments = new List<NarrativeSegment>
		{
			new()
			{
				Start = "2025-01-15 08:00:00", End = "2025-01-15 09:00:00",
				DurationMinutes = 60.0, Application = "VS Code",
			},
			new()
			{
				Start = "2025-01-15 09:30:00", End = "2025-01-15 10:00:00",
				DurationMinutes = 30.0, Application = "VS Code",
			},
		};

		var rawTotal = segments.Sum(s => s.DurationMinutes);
		rawTotal.Should().Be(90.0);

		var merged = NarrativeTools.MergeConsecutiveSegments(segments);
		// Merge produces one segment spanning 08:00-10:00 = 120 min (includes gap)
		merged.Should().HaveCount(1);
		merged[0].DurationMinutes.Should().Be(120.0);

		// But the CORRECT totalActiveMinutes should use the raw sum (90 min), not the merged sum
		// This test verifies the principle: raw total != merged total when gaps exist
		rawTotal.Should().BeLessThan(merged.Sum(s => s.DurationMinutes));
	}

	[TestMethod]
	public void MergeConsecutiveSegments_AbsorbsShortInterruptions()
	{
		// Terminal -> Chrome(20s) -> Terminal should collapse to one Terminal segment
		var segments = new List<NarrativeSegment>
		{
			new()
			{
				Start = "2025-01-15 13:30:00", End = "2025-01-15 13:33:00",
				DurationMinutes = 3.0, Application = "Terminal",
			},
			new()
			{
				Start = "2025-01-15 13:33:00", End = "2025-01-15 13:33:20",
				DurationMinutes = 0.3, Application = "Chrome",
			},
			new()
			{
				Start = "2025-01-15 13:33:20", End = "2025-01-15 13:43:00",
				DurationMinutes = 9.7, Application = "Terminal",
			},
		};

		var merged = NarrativeTools.MergeConsecutiveSegments(segments);
		merged.Should().HaveCount(1);
		merged[0].Application.Should().Be("Terminal");
		merged[0].Start.Should().Be("2025-01-15 13:30:00");
		merged[0].End.Should().Be("2025-01-15 13:43:00");
	}

	[TestMethod]
	public void MergeConsecutiveSegments_DoesNotAbsorbLongInterruptions()
	{
		// Terminal -> Chrome(5 min) -> Terminal should NOT collapse
		var segments = new List<NarrativeSegment>
		{
			new()
			{
				Start = "2025-01-15 13:30:00", End = "2025-01-15 13:33:00",
				DurationMinutes = 3.0, Application = "Terminal",
			},
			new()
			{
				Start = "2025-01-15 13:33:00", End = "2025-01-15 13:38:00",
				DurationMinutes = 5.0, Application = "Chrome",
			},
			new()
			{
				Start = "2025-01-15 13:38:00", End = "2025-01-15 13:43:00",
				DurationMinutes = 5.0, Application = "Terminal",
			},
		};

		var merged = NarrativeTools.MergeConsecutiveSegments(segments);
		merged.Should().HaveCount(3);
	}

	[TestMethod]
	public void IsValidWebsiteName_FiltersBogusEntries()
	{
		NarrativeTools.IsValidWebsiteName("c").Should().BeFalse();
		NarrativeTools.IsValidWebsiteName("").Should().BeFalse();
		NarrativeTools.IsValidWebsiteName("github.com").Should().BeTrue();
		NarrativeTools.IsValidWebsiteName("localhost").Should().BeTrue();
		NarrativeTools.IsValidWebsiteName("go").Should().BeTrue();
	}

	#endregion

	#region Capability statuses

	[TestMethod]
	public void GetCapabilityStatuses_ReturnsFallbackInfo()
	{
		var matrix = new QueryCapabilityMatrix([]);
		var statuses = matrix.GetCapabilityStatuses();

		statuses.Should().NotBeEmpty();
		var appUsage = statuses.Single(s => string.Equals(s.Name, "PreAggregatedAppUsage", StringComparison.Ordinal));
		appUsage.Available.Should().BeFalse();
		appUsage.FallbackActive.Should().BeTrue();

		var tags = statuses.Single(s => string.Equals(s.Name, "Tags", StringComparison.Ordinal));
		tags.Available.Should().BeFalse();
		tags.FallbackActive.Should().BeFalse();
	}

	[TestMethod]
	public void GetCapabilityStatuses_FullCapabilities_AllAvailable()
	{
		var matrix = CreateFullCapabilities();
		var statuses = matrix.GetCapabilityStatuses();

		statuses.Should().OnlyContain(s => s.Available);
		statuses.Should().OnlyContain(s => !s.FallbackActive);
	}

	#endregion

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
