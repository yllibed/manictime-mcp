using AwesomeAssertions;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Mcp.Models;
using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class NarrativeToolsTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
		new() { ReportId = 2, SchemaName = "ManicTime/Documents", BaseSchemaName = "ManicTime/Documents" },
		new() { ReportId = 3, SchemaName = "ManicTime/ComputerUsage", BaseSchemaName = "ManicTime/ComputerUsage" },
	];

	private static readonly ActivityDto[] SampleActivities =
	[
		new() { ActivityId = 1, ReportId = 1, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00", Name = "VS Code", GroupId = null },
		new() { ActivityId = 2, ReportId = 1, StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 10:00:00", Name = "Chrome", GroupId = null },
		new() { ActivityId = 10, ReportId = 2, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:30:00", Name = "Program.cs", GroupId = null },
		new() { ActivityId = 11, ReportId = 3, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 10:00:00", Name = "Active", GroupId = null },
	];

	private static readonly EnrichedActivityDto[] SampleEnrichedActivities =
	[
		new()
		{
			ActivityId = 1,
			ReportId = 1,
			StartLocalTime = "2025-01-15 08:00:00",
			EndLocalTime = "2025-01-15 09:00:00",
			Name = "VS Code",
			GroupId = null,
			CommonGroupName = "Visual Studio Code",
		},
		new()
		{
			ActivityId = 2,
			ReportId = 1,
			StartLocalTime = "2025-01-15 09:00:00",
			EndLocalTime = "2025-01-15 10:00:00",
			Name = "Chrome",
			GroupId = null,
			CommonGroupName = "Google Chrome",
		},
	];

	private static readonly DailyUsageDto[] SampleDailyAppUsage =
	[
		new() { Day = "2025-01-15", Name = "VS Code", Color = "#007ACC", Key = "code.exe", TotalSeconds = 3600 },
		new() { Day = "2025-01-15", Name = "Chrome", Color = "#4285F4", Key = "chrome.exe", TotalSeconds = 1800 },
	];

	private static readonly DailyUsageDto[] SampleDailyWebUsage =
	[
		new() { Day = "2025-01-15", Name = "github.com", TotalSeconds = 1200 },
		new() { Day = "2025-01-15", Name = "stackoverflow.com", TotalSeconds = 600 },
	];

	private static readonly HourlyUsageDto[] SampleHourlyAppUsage =
	[
		new() { Day = "2025-01-15", Hour = 8, Name = "VS Code", Color = "#007ACC", Key = "code.exe", TotalSeconds = 3600 },
		new() { Day = "2025-01-15", Hour = 17, Name = "Chrome", Color = "#4285F4", Key = "chrome.exe", TotalSeconds = 1800 },
	];

	private static readonly HourlyUsageDto[] SampleHourlyWebUsage =
	[
		new() { Day = "2025-01-15", Hour = 9, Name = "github.com", TotalSeconds = 1200 },
		new() { Day = "2025-01-15", Hour = 10, Name = "brief-site.com", TotalSeconds = 20 },
	];

	[TestMethod]
	public async Task GetActivityNarrativeAsync_ReturnsSegmentsAndSummaries()
	{
		var tools = CreateTools();

		var result = await tools.GetActivityNarrativeAsync("2025-01-15", "2025-01-16", includeSummary: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("segments").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("topApplications").GetArrayLength().Should().Be(2);
		doc.RootElement.GetProperty("topWebsites").GetArrayLength().Should().Be(2);
	}

	[TestMethod]
	public async Task GetDailySummaryAsync_IncludeSegmentsFalse_OmitsSegmentsButKeepsTotals()
	{
		var tools = CreateTools();

		var result = await tools.GetDailySummaryAsync("2025-01-15", includeSegments: false, cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("segments").GetArrayLength().Should().Be(0);
		doc.RootElement.GetProperty("topApplications").GetArrayLength().Should().BeGreaterThan(0);
		doc.RootElement.GetProperty("totalActiveMinutes").GetDouble().Should().BeGreaterThan(0);
	}

	[TestMethod]
	public async Task GetPeriodSummaryAsync_ReturnsDayBreakdownAndFirstLastActivity()
	{
		var tools = CreateTools(hourlyAppUsage: SampleHourlyAppUsage);

		var result = await tools.GetPeriodSummaryAsync("2025-01-15", "2025-01-16", CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		var day = doc.RootElement.GetProperty("days")[0];
		day.GetProperty("firstActivity").GetString().Should().Be("VS Code");
		day.GetProperty("lastActivity").GetString().Should().Be("Chrome");
	}

	[TestMethod]
	public async Task GetPeriodSummaryAsync_RangeTooLarge_ReturnsError()
	{
		var tools = CreateTools();

		var result = await tools.GetPeriodSummaryAsync("2025-01-01", "2025-03-01", CancellationToken.None).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		result.Payload.Should().Contain("maximum");
	}

	[TestMethod]
	public async Task GetWebsiteUsageAsync_DefaultMinMinutes_FiltersBriefVisits()
	{
		var tools = CreateTools(hourlyWebUsage: SampleHourlyWebUsage);

		var result = await tools.GetWebsiteUsageAsync("2025-01-15", "2025-01-16", cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		var names = doc.RootElement.GetProperty("websites")
			.EnumerateArray()
			.Select(static website => website.GetProperty("name").GetString())
			.ToList();
		names.Should().Contain("github.com");
		names.Should().NotContain("brief-site.com");
	}

	[TestMethod]
	public async Task GetActivityNarrativeAsync_AssignsScreenshotRefsWhenAvailable()
	{
		var registry = new ScreenshotRegistry();
		var screenshot = new ScreenshotInfo
		{
			Date = new DateOnly(2025, 1, 15),
			Time = new TimeOnly(8, 30, 0),
			Offset = "+00-00",
			Width = 1920,
			Height = 1080,
			Sequence = 0,
			Monitor = 0,
			IsThumbnail = true,
			FilePath = @"C:\Data\Screenshots\2025-01-15\test.thumbnail.jpg",
		};
		var screenshotRef = registry.Register(screenshot);
		screenshot = screenshot with { Ref = screenshotRef };

		var tools = CreateTools(
			screenshotService: new StubScreenshotService(new ScreenshotSelection
			{
				Screenshots = [screenshot],
				TotalMatching = 1,
				IsTruncated = false,
			}),
			screenshotRegistry: registry);

		var result = await tools.GetActivityNarrativeAsync("2025-01-15", "2025-01-16", cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("segments")[0].GetProperty("screenshotRef").GetString().Should().NotBeNullOrWhiteSpace();
	}

	[TestMethod]
	public void MergeConsecutiveSegments_MergesSameAppWithinGap()
	{
		var segments = new List<NarrativeSegment>
		{
			new() { Start = "2025-01-15 08:00:00", End = "2025-01-15 09:00:00", DurationMinutes = 60.0, Application = "VS Code" },
			new() { Start = "2025-01-15 09:01:00", End = "2025-01-15 10:00:00", DurationMinutes = 59.0, Application = "VS Code" },
		};

		var merged = NarrativeTools.MergeConsecutiveSegments(segments);

		merged.Should().HaveCount(1);
		merged[0].DurationMinutes.Should().Be(120.0);
	}

	[TestMethod]
	public void MergeConsecutiveSegments_AbsorbsShortInterruptions()
	{
		var segments = new List<NarrativeSegment>
		{
			new() { Start = "2025-01-15 13:30:00", End = "2025-01-15 13:33:00", DurationMinutes = 3.0, Application = "Terminal" },
			new() { Start = "2025-01-15 13:33:00", End = "2025-01-15 13:33:20", DurationMinutes = 0.3, Application = "Chrome" },
			new() { Start = "2025-01-15 13:33:20", End = "2025-01-15 13:43:00", DurationMinutes = 9.7, Application = "Terminal" },
		};

		var merged = NarrativeTools.MergeConsecutiveSegments(segments);

		merged.Should().HaveCount(1);
		merged[0].Application.Should().Be("Terminal");
	}

	[TestMethod]
	public void SanitizeDocumentName_NormalizesWindowsPath()
	{
		NarrativeTools.SanitizeDocumentName(@"C:\Users\test\file.cs")
			.Should().Be("file:///C:/Users/test/file.cs");
	}

	[TestMethod]
	public void IsValidWebsiteName_FiltersBogusNames()
	{
		NarrativeTools.IsValidWebsiteName("c").Should().BeFalse();
		NarrativeTools.IsValidWebsiteName("github.com").Should().BeTrue();
	}

	[TestMethod]
	public void ClipToActiveIntervals_ExcludesAwayTime()
	{
		var activities = new[]
		{
			new EnrichedActivityDto
			{
				ActivityId = 1,
				ReportId = 1,
				StartLocalTime = "2025-01-15 08:00:00",
				EndLocalTime = "2025-01-15 10:00:00",
				Name = "VS Code",
				GroupId = null,
			},
		};

		var usageActivities = new ActivityDto[]
		{
			new() { ActivityId = 100, ReportId = 3, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00", Name = "Active", GroupId = null },
			new() { ActivityId = 101, ReportId = 3, StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 09:30:00", Name = "Away", GroupId = null },
			new() { ActivityId = 102, ReportId = 3, StartLocalTime = "2025-01-15 09:30:00", EndLocalTime = "2025-01-15 10:00:00", Name = "Active", GroupId = null },
		};

		var result = NarrativeTools.ClipToActiveIntervals(activities, usageActivities);

		result.Should().HaveCount(2);
		result[0].EndLocalTime.Should().Be("2025-01-15 09:00:00");
		result[1].StartLocalTime.Should().Be("2025-01-15 09:30:00");
	}

	private static NarrativeTools CreateTools(
		IReadOnlyList<EnrichedActivityDto>? enrichedActivities = null,
		IReadOnlyList<HourlyUsageDto>? hourlyAppUsage = null,
		IReadOnlyList<HourlyUsageDto>? hourlyWebUsage = null,
		IScreenshotService? screenshotService = null,
		IScreenshotRegistry? screenshotRegistry = null) =>
		new(
			new StubActivityRepository(SampleActivities, enrichedActivities ?? SampleEnrichedActivities),
			new StubTimelineRepository(SampleTimelines),
			new StubUsageRepository(
				dailyApp: SampleDailyAppUsage,
				dailyWeb: SampleDailyWebUsage,
				hourlyApp: hourlyAppUsage,
				hourlyWeb: hourlyWebUsage),
			CreateFullCapabilities(),
			screenshotService,
			screenshotRegistry);

	private static QueryCapabilityMatrix CreateFullCapabilities() =>
		new(
		[
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
			"Ar_TagListByDay",
			"Ar_TagListByYear",
			"Ar_Category",
			"Ar_CategoryGroup",
		]);
}
