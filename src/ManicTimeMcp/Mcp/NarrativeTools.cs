using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp.Models;
using ManicTimeMcp.Screenshots;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP tools for narrative and summary queries.</summary>
[McpServerToolType]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class NarrativeTools
{
	private const int DefaultMaxSegments = 200;
	private const int MaxSegmentsLimit = 2000;
	private const int MaxTopApps = 50;
	private const int MaxTopWebsites = 50;
	private const int MaxPeriodDays = 31;
	private const int MaxWebsites = 200;

	private readonly IActivityRepository _activityRepository;
	private readonly ITimelineRepository _timelineRepository;
	private readonly IUsageRepository _usageRepository;
	private readonly IScreenshotService? _screenshotService;
	private readonly IScreenshotRegistry? _screenshotRegistry;
	private readonly QueryCapabilityMatrix _capabilities;

	/// <summary>Creates narrative tools with injected repositories.</summary>
	public NarrativeTools(
		IActivityRepository activityRepository,
		ITimelineRepository timelineRepository,
		IUsageRepository usageRepository,
		QueryCapabilityMatrix capabilities,
		IScreenshotService? screenshotService = null,
		IScreenshotRegistry? screenshotRegistry = null)
	{
		_activityRepository = activityRepository;
		_timelineRepository = timelineRepository;
		_usageRepository = usageRepository;
		_capabilities = capabilities;
		_screenshotService = screenshotService;
		_screenshotRegistry = screenshotRegistry;
	}

	/// <summary>Returns a daily summary by delegating to narrative logic.</summary>
	[McpServerTool(Name = "get_daily_summary", ReadOnly = true), Description("Get a structured summary of activity for a specific date, with segments, top applications, and websites. Includes suggested screenshots for visual context — use get_screenshot to fetch them.")]
	public async Task<CallToolResult> GetDailySummaryAsync(
		[Description("Date to summarize (ISO-8601, e.g. 2025-01-15)")] string date,
		[Description("Include activity segments in response (default true). Set false to reduce payload when only summary data needed.")] bool includeSegments = true,
		[Description("Minimum segment duration in minutes to include (default 0). Useful for filtering noise — e.g. 0.5 filters sub-30s segments.")] double minDurationMinutes = 0,
		[Description("Include hourly website breakdown (default false). Adds per-hour detail for each website.")] bool includeHourlyWebBreakdown = false,
		[Description("Maximum gap in minutes between same-app segments to merge them (default 2). Reduces segment count by merging nearby blocks of the same application.")] double maxGapMinutes = 2.0,
		[Description("Maximum number of segments to return (default 200, max 2000). Increase for full-day detail.")] int? maxSegments = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var parsed = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var endDate = parsed.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			var response = await BuildNarrativeAsync(date, endDate, includeWebsites: true, includeSegments, minDurationMinutes, includeHourlyWebBreakdown, maxGapMinutes, includeSummary: true, maxSegments, cancellationToken).ConfigureAwait(false);
			return ToolResults.Success(JsonSerializer.Serialize(response, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	/// <summary>Returns a structured narrative for "what did I do?"</summary>
	[McpServerTool(Name = "get_activity_narrative", ReadOnly = true), Description("Get a structured narrative of activities for a date range. Best for single-day 'what did I do?' queries. Includes suggested screenshots when available.")]
	public async Task<CallToolResult> GetActivityNarrativeAsync(
		[Description("Start date (ISO-8601, inclusive)")] string startDate,
		[Description("End date (ISO-8601, exclusive)")] string endDate,
		[Description("Include website usage (default true)")] bool includeWebsites = true,
		[Description("Minimum segment duration in minutes to include (default 0). Useful for filtering noise — e.g. 0.5 filters sub-30s segments.")] double minDurationMinutes = 0,
		[Description("Maximum gap in minutes between same-app segments to merge them (default 2). Reduces segment count by merging nearby blocks of the same application.")] double maxGapMinutes = 2.0,
		[Description("Include topApplications and topWebsites summary data (default false). Set true when you need aggregated top-app/top-website lists alongside segments.")] bool includeSummary = false,
		[Description("Maximum number of segments to return (default 200, max 2000). Increase for full-day detail.")] int? maxSegments = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var response = await BuildNarrativeAsync(startDate, endDate, includeWebsites, includeSegments: true, minDurationMinutes, includeHourlyWebBreakdown: false, maxGapMinutes, includeSummary, maxSegments, cancellationToken).ConfigureAwait(false);
			return ToolResults.Success(JsonSerializer.Serialize(response, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	/// <summary>Returns a multi-day period summary.</summary>
	[McpServerTool(Name = "get_period_summary", ReadOnly = true), Description("Get a multi-day summary with per-day breakdown and day-of-week patterns. Best for weekly/monthly overviews.")]
	public async Task<CallToolResult> GetPeriodSummaryAsync(
		[Description("Start date (ISO-8601, inclusive)")] string startDate,
		[Description("End date (ISO-8601, exclusive, max 31 days from start)")] string endDate,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var (start, end) = ParseDates(startDate, endDate);
			if ((end - start).Days > MaxPeriodDays)
			{
				return ToolResults.Error($"Date range exceeds maximum of {MaxPeriodDays} days.");
			}

			var response = await BuildPeriodSummaryAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
			return ToolResults.Success(JsonSerializer.Serialize(response, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	/// <summary>Returns website usage with hourly or daily breakdown.</summary>
	[McpServerTool(Name = "get_website_usage", ReadOnly = true), Description("Get website usage for a date range with hourly (<=7 days) or daily (>7 days) breakdown.")]
	public async Task<CallToolResult> GetWebsiteUsageAsync(
		[Description("Start date (ISO-8601, inclusive)")] string startDate,
		[Description("End date (ISO-8601, exclusive, max 31 days)")] string endDate,
		[Description("Maximum number of websites (default 50, max 200)")] int? limit = null,
		[Description("Minimum total minutes to include a website (default 0.5). Filters sub-30s visits. Set 0 to include all.")] double minMinutes = 0.5,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var (start, end) = ParseDates(startDate, endDate);
			var rangeDays = (end - start).Days;
			if (rangeDays > MaxPeriodDays)
			{
				return ToolResults.Error($"Date range exceeds maximum of {MaxPeriodDays} days.");
			}

			var response = await BuildWebsiteUsageAsync(startDate, endDate, rangeDays, limit, minMinutes, cancellationToken).ConfigureAwait(false);
			return ToolResults.Success(JsonSerializer.Serialize(response, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	private async Task<NarrativeResponse> BuildNarrativeAsync(
		string startDate, string endDate, bool includeWebsites, bool includeSegments,
		double minDurationMinutes, bool includeHourlyWebBreakdown, double maxGapMinutes,
		bool includeSummary = true, int? maxSegments = null, CancellationToken ct = default)
	{
		var (segments, totalMinutes, totalSegments, isTruncated) =
			await BuildSegmentDataAsync(startDate, endDate, includeSegments, minDurationMinutes, maxGapMinutes, maxSegments, ct).ConfigureAwait(false);

		var suggestedScreenshots = ScreenshotSuggestionSelector.Select(segments);

		var topApps = includeSummary
			? await GetTopAppsAsync(startDate, endDate, ct).ConfigureAwait(false)
			: [];
		var topWebsites = includeSummary && includeWebsites
			? await GetTopWebsitesAsync(startDate, endDate, ct).ConfigureAwait(false)
			: null;

		var hourlyWeb = includeHourlyWebBreakdown
			? await BuildHourlyWebBreakdownAsync(startDate, endDate, MaxTopWebsites, ct).ConfigureAwait(false)
			: null;

		return new NarrativeResponse
		{
			StartDate = startDate,
			EndDate = endDate,
			TotalActiveMinutes = Math.Round(totalMinutes, digits: 1),
			Segments = segments,
			TopApplications = topApps,
			TopWebsites = topWebsites,
			SuggestedScreenshots = suggestedScreenshots,
			HourlyWebBreakdown = hourlyWeb is { Count: > 0 } ? hourlyWeb : null,
			Truncation = new TruncationInfo
			{
				Truncated = isTruncated,
				ReturnedCount = segments.Count,
				TotalAvailable = totalSegments,
			},
			Diagnostics = BuildDiagnostics(_capabilities.HasPreAggregatedAppUsage),
		};
	}

	private async Task<(List<NarrativeSegment> Segments, double TotalMinutes, int TotalSegments, bool IsTruncated)>
		BuildSegmentDataAsync(string startDate, string endDate, bool includeSegments,
			double minDurationMinutes, double maxGapMinutes, int? maxSegments, CancellationToken ct)
	{
		if (!includeSegments)
		{
			var appUsage = await _usageRepository.GetDailyAppUsageAsync(
				startDate, endDate, cancellationToken: ct).ConfigureAwait(false);
			var total = Math.Round(appUsage.Sum(a => a.TotalSeconds) / 60.0, digits: 1);
			return ([], total, 0, false);
		}

		var startLocal = FormatLocalTime(ParseDates(startDate, endDate).Start);
		var endLocal = endDate + " 00:00:00";

		var rawSegments = await BuildSegmentsAsync(startLocal, endLocal, ct).ConfigureAwait(false);
		var segments = MergeConsecutiveSegments(rawSegments, maxGapMinutes);

		if (minDurationMinutes > 0)
		{
			segments = segments.Where(s => s.DurationMinutes >= minDurationMinutes).ToList();
		}

		var totalMinutes = segments.Sum(s => s.DurationMinutes);
		var totalSegments = segments.Count;
		var effectiveMaxSegments = Math.Min(maxSegments ?? DefaultMaxSegments, MaxSegmentsLimit);
		var isTruncated = segments.Count > effectiveMaxSegments;
		if (isTruncated)
		{
			segments = segments.Take(effectiveMaxSegments).ToList();
		}

		return (segments, totalMinutes, totalSegments, isTruncated);
	}

	private async Task<PeriodSummaryResponse> BuildPeriodSummaryAsync(
		string startDate, string endDate, CancellationToken ct)
	{
		var dailyUsage = await _usageRepository.GetDailyAppUsageAsync(
			startDate, endDate, cancellationToken: ct).ConfigureAwait(false);
		var hourlyUsage = await _usageRepository.GetHourlyAppUsageAsync(
			startDate, endDate, cancellationToken: ct).ConfigureAwait(false);

		var dayEntries = BuildDayEntries(dailyUsage, hourlyUsage);
		var topApps = BuildTopApps(dailyUsage);
		var topWebsites = await BuildTopWebsitesAggregateAsync(startDate, endDate, ct).ConfigureAwait(false);
		var dowDistribution = await BuildDayOfWeekDistributionAsync(startDate, endDate, ct).ConfigureAwait(false);

		var avgDaily = dayEntries.Count > 0 ? Math.Round(dayEntries.Average(d => d.TotalActiveMinutes), digits: 1) : 0;
		var busiest = dayEntries.OrderByDescending(d => d.TotalActiveMinutes).FirstOrDefault();
		var quietest = dayEntries.OrderBy(d => d.TotalActiveMinutes).FirstOrDefault();

		return new PeriodSummaryResponse
		{
			Days = dayEntries,
			Aggregate = new PeriodAggregate
			{
				TopApps = topApps,
				TopWebsites = topWebsites.Count > 0 ? topWebsites : null,
				AvgDailyMinutes = avgDaily,
				BusiestDay = busiest?.Date,
				QuietestDay = quietest?.Date,
			},
			Patterns = new PeriodPatterns { DayOfWeekDistribution = dowDistribution },
			Truncation = new TruncationInfo
			{
				Truncated = false,
				ReturnedCount = dayEntries.Count,
				TotalAvailable = dayEntries.Count,
			},
			Diagnostics = BuildDiagnostics(_capabilities.HasPreAggregatedAppUsage),
		};
	}

	private async Task<WebsiteUsageResponse> BuildWebsiteUsageAsync(
		string startDate, string endDate, int rangeDays, int? limit, double minMinutes, CancellationToken ct)
	{
		var effectiveLimit = Math.Min(limit ?? MaxTopWebsites, MaxWebsites);
		var useHourly = rangeDays <= 7;

		var websites = useHourly
			? await BuildHourlyWebBreakdownAsync(startDate, endDate, effectiveLimit, ct).ConfigureAwait(false)
			: await BuildDailyWebBreakdownAsync(startDate, endDate, effectiveLimit, ct).ConfigureAwait(false);

		if (minMinutes > 0)
		{
			websites = websites.Where(w => w.TotalMinutes >= minMinutes).ToList();
		}

		return new WebsiteUsageResponse
		{
			BreakdownGranularity = useHourly ? "hour" : "day",
			Websites = websites,
			Truncation = new TruncationInfo
			{
				Truncated = websites.Count >= effectiveLimit,
				ReturnedCount = websites.Count,
				TotalAvailable = null,
			},
			Diagnostics = BuildDiagnostics(_capabilities.HasPreAggregatedWebUsage),
		};
	}

	private async Task<List<NarrativeSegment>> BuildSegmentsAsync(
		string startLocal, string endLocal, CancellationToken ct)
	{
		var timelines = await _timelineRepository.GetTimelinesAsync(ct).ConfigureAwait(false);
		var appTimeline = timelines.FirstOrDefault(
			t => t.SchemaName.Equals("ManicTime/Applications", StringComparison.OrdinalIgnoreCase) ||
				 t.BaseSchemaName.Equals("ManicTime/Applications", StringComparison.OrdinalIgnoreCase));

		if (appTimeline is null)
		{
			return [];
		}

		var activitiesTask = _activityRepository.GetEnrichedActivitiesAsync(
			appTimeline.ReportId, startLocal, endLocal,
			limit: QueryLimits.MaxActivities, cancellationToken: ct);

		// Find ComputerUsage timeline for Active/Away clipping
		var usageTimeline = timelines.FirstOrDefault(
			t => t.SchemaName.Equals("ManicTime/ComputerUsage", StringComparison.OrdinalIgnoreCase) ||
				 t.BaseSchemaName.Equals("ManicTime/ComputerUsage", StringComparison.OrdinalIgnoreCase));

		var usageActivitiesTask = usageTimeline is not null
			? _activityRepository.GetActivitiesAsync(
				usageTimeline.ReportId, startLocal, endLocal,
				limit: QueryLimits.MaxActivities, cancellationToken: ct)
			: Task.FromResult<IReadOnlyList<Database.Dto.ActivityDto>>([]);

		// Find Documents timeline for cross-timeline correlation
		var docTimeline = timelines.FirstOrDefault(
			t => t.SchemaName.Equals("ManicTime/Documents", StringComparison.OrdinalIgnoreCase) ||
				 t.BaseSchemaName.Equals("ManicTime/Documents", StringComparison.OrdinalIgnoreCase));

		var docActivitiesTask = docTimeline is not null
			? _activityRepository.GetActivitiesAsync(
				docTimeline.ReportId, startLocal, endLocal,
				limit: QueryLimits.MaxActivities, cancellationToken: ct)
			: Task.FromResult<IReadOnlyList<Database.Dto.ActivityDto>>([]);

		var webActivitiesTask = docTimeline is not null
			? _activityRepository.GetActivitiesWithGroupTypeAsync(
				docTimeline.ReportId, startLocal, endLocal,
				groupType: "ManicTime/WebSites",
				limit: QueryLimits.MaxActivities, cancellationToken: ct)
			: Task.FromResult<IReadOnlyList<Database.Dto.ActivityDto>>([]);

		var activities = await activitiesTask.ConfigureAwait(false);
		var usageActivities = await usageActivitiesTask.ConfigureAwait(false);
		var docActivities = await docActivitiesTask.ConfigureAwait(false);
		var webActivities = await webActivitiesTask.ConfigureAwait(false);

		// Clip application activities to Active intervals (excludes Away/Locked/Off time)
		var clippedActivities = ClipToActiveIntervals(activities, usageActivities);

		var screenshots = await LoadScreenshotsAsync(startLocal, endLocal, ct).ConfigureAwait(false);

		return MapToSegments(clippedActivities, docActivities, webActivities, screenshots);
	}

	/// <summary>
	/// Clips enriched activities to only the intervals where the computer was Active.
	/// Activities spanning Away/Locked/Off periods are split at Active boundaries.
	/// If no ComputerUsage data is available, returns the original activities unchanged.
	/// </summary>
	internal static IReadOnlyList<Database.Dto.EnrichedActivityDto> ClipToActiveIntervals(
		IReadOnlyList<Database.Dto.EnrichedActivityDto> activities,
		IReadOnlyList<Database.Dto.ActivityDto> usageActivities)
	{
		if (usageActivities.Count == 0)
		{
			return activities; // Graceful degradation: no usage data → skip clipping
		}

		// Build active intervals from ComputerUsage timeline
		var activeIntervals = new List<(DateTime Start, DateTime End)>();
		foreach (var usage in usageActivities)
		{
			if (string.Equals(usage.Name, "Active", StringComparison.OrdinalIgnoreCase))
			{
				var start = DateTime.ParseExact(usage.StartLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				var end = DateTime.ParseExact(usage.EndLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				activeIntervals.Add((start, end));
			}
		}

		if (activeIntervals.Count == 0)
		{
			return []; // Entire period was Away/Locked/Off
		}

		var result = new List<Database.Dto.EnrichedActivityDto>();

		foreach (var activity in activities)
		{
			var actStart = DateTime.ParseExact(activity.StartLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			var actEnd = DateTime.ParseExact(activity.EndLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

			foreach (var (intervalStart, intervalEnd) in activeIntervals)
			{
				if (intervalStart >= actEnd)
				{
					break; // Active intervals are sorted; no more overlaps possible
				}

				if (intervalEnd <= actStart)
				{
					continue;
				}

				// Compute intersection
				var clippedStart = actStart > intervalStart ? actStart : intervalStart;
				var clippedEnd = actEnd < intervalEnd ? actEnd : intervalEnd;

				if (clippedEnd > clippedStart)
				{
					result.Add(activity with
					{
						StartLocalTime = clippedStart.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
						EndLocalTime = clippedEnd.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					});
				}
			}
		}

		return result;
	}

	private async Task<IReadOnlyList<ScreenshotInfo>?> LoadScreenshotsAsync(
		string startLocal, string endLocal, CancellationToken ct)
	{
		if (_screenshotService is null || _screenshotRegistry is null)
		{
			return null;
		}

		var startDt = DateTime.ParseExact(startLocal, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		var endDt = DateTime.ParseExact(endLocal, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		var selection = await _screenshotService.ListScreenshotsAsync(
			new ScreenshotQuery
			{
				StartLocalTime = startDt,
				EndLocalTime = endDt,
				PreferThumbnails = true,
			}, ct).ConfigureAwait(false);
		return selection.Screenshots;
	}

	/// <summary>Known browser application names for website carry-forward logic.</summary>
	private static readonly HashSet<string> BrowserAppNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Google Chrome", "Chrome", "Mozilla Firefox", "Firefox",
		"Microsoft Edge", "Edge", "Brave Browser", "Brave",
		"Opera", "Safari", "Vivaldi", "Arc",
	};

	private static List<NarrativeSegment> MapToSegments(
		IReadOnlyList<Database.Dto.EnrichedActivityDto> activities,
		IReadOnlyList<Database.Dto.ActivityDto> docActivities,
		IReadOnlyList<Database.Dto.ActivityDto> webActivities,
		IReadOnlyList<ScreenshotInfo>? screenshots)
	{
		string? lastKnownWebsite = null;
		return activities
			.Select(a =>
			{
				var startDt = DateTime.ParseExact(a.StartLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				var endDt = DateTime.ParseExact(a.EndLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				var appName = a.CommonGroupName ?? a.GroupName ?? a.Name;
				var website = FindOverlappingWebsite(webActivities, startDt, endDt);

				// Carry forward last known website for browser segments with no direct match
				var isBrowser = appName is not null && BrowserAppNames.Contains(appName);
				if (website is not null)
				{
					lastKnownWebsite = website;
				}
				else if (isBrowser && lastKnownWebsite is not null)
				{
					website = lastKnownWebsite;
				}

				return new NarrativeSegment
				{
					Start = a.StartLocalTime,
					End = a.EndLocalTime,
					DurationMinutes = Math.Round((endDt - startDt).TotalMinutes, digits: 1),
					Application = appName,
					Document = SanitizeDocumentName(FindOverlappingDocument(docActivities, startDt, endDt)),
					Website = website,
					Tags = a.Tags is { Length: > 0 } ? a.Tags : null,
					ScreenshotRef = FindClosestScreenshot(screenshots, startDt, endDt),
				};
			})
			.ToList();
	}

	/// <summary>Tolerance for website matching — allows a website activity that ended
	/// shortly before the segment started to still count as a match.</summary>
	private static readonly TimeSpan WebsiteMatchTolerance = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Finds the website name from the WebSites activities that overlaps the given time range.
	/// Uses a small tolerance to catch website activities that ended just before the segment.
	/// </summary>
	private static string? FindOverlappingWebsite(
		IReadOnlyList<Database.Dto.ActivityDto> webActivities, DateTime segStart, DateTime segEnd)
	{
		if (webActivities.Count == 0)
		{
			return null;
		}

		string? bestName = null;
		var bestOverlap = TimeSpan.Zero;

		foreach (var web in webActivities)
		{
			var webStart = DateTime.ParseExact(web.StartLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			if (webStart >= segEnd)
			{
				break; // Activities are sorted by start time; no more overlaps possible
			}

			var webEnd = DateTime.ParseExact(web.EndLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

			// Allow tolerance: website ended within tolerance before segment started
			if (webEnd.Add(WebsiteMatchTolerance) <= segStart)
			{
				continue;
			}

			var overlapStart = webStart > segStart ? webStart : segStart;
			var overlapEnd = webEnd < segEnd ? webEnd : segEnd;
			var overlap = overlapEnd - overlapStart;

			// If overlap is non-positive, the match is via tolerance only — use a minimal positive overlap
			if (overlap <= TimeSpan.Zero)
			{
				overlap = TimeSpan.FromTicks(1);
			}

			if (overlap > bestOverlap)
			{
				bestOverlap = overlap;
				bestName = web.Name;
			}
		}

		return bestName;
	}

	/// <summary>
	/// Finds the document name from the Documents timeline that overlaps the given time range.
	/// Returns the name of the document with the most overlap, or null if none found.
	/// </summary>
	private static string? FindOverlappingDocument(
		IReadOnlyList<Database.Dto.ActivityDto> docActivities, DateTime segStart, DateTime segEnd)
	{
		if (docActivities.Count == 0)
		{
			return null;
		}

		string? bestName = null;
		var bestOverlap = TimeSpan.Zero;

		foreach (var doc in docActivities)
		{
			var docStart = DateTime.ParseExact(doc.StartLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			if (docStart >= segEnd)
			{
				break; // Activities are sorted by start time; no more overlaps possible
			}

			var docEnd = DateTime.ParseExact(doc.EndLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			if (docEnd <= segStart)
			{
				continue;
			}

			var overlapStart = docStart > segStart ? docStart : segStart;
			var overlapEnd = docEnd < segEnd ? docEnd : segEnd;
			var overlap = overlapEnd - overlapStart;
			if (overlap > bestOverlap)
			{
				bestOverlap = overlap;
				bestName = doc.Name;
			}
		}

		return bestName;
	}

	/// <summary>
	/// Finds the screenshot closest to the midpoint of a segment's time range.
	/// Returns the screenshot's registered ref string, or null if no screenshots available.
	/// </summary>
	private static string? FindClosestScreenshot(
		IReadOnlyList<ScreenshotInfo>? screenshots, DateTime segStart, DateTime segEnd)
	{
		if (screenshots is null or { Count: 0 })
		{
			return null;
		}

		var midpoint = segStart.AddTicks((segEnd - segStart).Ticks / 2);
		ScreenshotInfo? closest = null;
		var closestDistance = TimeSpan.MaxValue;

		foreach (var shot in screenshots)
		{
			var distance = (shot.LocalTimestamp - midpoint).Duration();
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closest = shot;
			}
			else if (shot.LocalTimestamp > segEnd)
			{
				break; // Screenshots past the segment end won't be closer
			}
		}

		return closest?.Ref;
	}

	/// <summary>
	/// Merges consecutive segments with the same application (within a gap threshold),
	/// then absorbs short interruptions (&lt;30s) between same-app segments.
	/// </summary>
	/// <param name="segments">Ordered segments to merge.</param>
	/// <param name="maxGapMinutes">Maximum gap in minutes between same-app segments to still merge them. Default 2 minutes.</param>
	internal static List<NarrativeSegment> MergeConsecutiveSegments(List<NarrativeSegment> segments, double maxGapMinutes = 2.0)
	{
		if (segments.Count <= 1)
		{
			return segments;
		}

		// Pass 1: merge consecutive same-app segments within gap threshold
		var merged = MergeConsecutiveSameApp(segments, maxGapMinutes);

		// Pass 2: absorb short interruptions between same-app segments
		return AbsorbShortInterruptions(merged);
	}

	private static List<NarrativeSegment> MergeConsecutiveSameApp(List<NarrativeSegment> segments, double maxGapMinutes)
	{
		var merged = new List<NarrativeSegment>(segments.Count);
		var current = segments[0];

		for (var i = 1; i < segments.Count; i++)
		{
			var next = segments[i];
			if (string.Equals(current.Application, next.Application, StringComparison.Ordinal)
				&& GapMinutes(current, next) <= maxGapMinutes)
			{
				current = MergeTwo(current, next);
			}
			else
			{
				merged.Add(current);
				current = next;
			}
		}

		merged.Add(current);
		return merged;
	}

	private static double GapMinutes(NarrativeSegment a, NarrativeSegment b)
	{
		var aEnd = DateTime.ParseExact(a.End, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		var bStart = DateTime.ParseExact(b.Start, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		return (bStart - aEnd).TotalMinutes;
	}

	/// <summary>
	/// Absorbs sub-30s interruptions between same-app segments.
	/// E.g. Terminal→Chrome(20s)→Terminal becomes one Terminal segment.
	/// </summary>
	private static List<NarrativeSegment> AbsorbShortInterruptions(List<NarrativeSegment> segments)
	{
		if (segments.Count < 3)
		{
			return segments;
		}

		bool changed;
		var result = segments;
		do
		{
			changed = false;
			var next = new List<NarrativeSegment>(result.Count);
			var i = 0;
			while (i < result.Count)
			{
				if (i + 2 < result.Count
					&& string.Equals(result[i].Application, result[i + 2].Application, StringComparison.Ordinal)
					&& result[i + 1].DurationMinutes < 0.5)
				{
					// Absorb: extend first segment to cover all three
					var combined = MergeTwo(MergeTwo(result[i], result[i + 1]), result[i + 2]);
					next.Add(combined);
					i += 3;
					changed = true;
				}
				else
				{
					next.Add(result[i]);
					i++;
				}
			}

			result = next;
		} while (changed && result.Count >= 3);

		return result;
	}

	private static NarrativeSegment MergeTwo(NarrativeSegment a, NarrativeSegment b)
	{
		var endDt = DateTime.ParseExact(b.End, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		var startDt = DateTime.ParseExact(a.Start, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		return new NarrativeSegment
		{
			Start = a.Start,
			End = b.End,
			DurationMinutes = Math.Round((endDt - startDt).TotalMinutes, digits: 1),
			Application = a.Application,
			Document = a.Document ?? b.Document,
			Website = a.Website ?? b.Website,
			Tags = MergeTags(a.Tags, b.Tags),
			ScreenshotRef = a.ScreenshotRef,
		};
	}

	private static string[]? MergeTags(string[]? a, string[]? b)
	{
		if (a is null or { Length: 0 })
		{
			return b is { Length: > 0 } ? b : null;
		}

		if (b is null or { Length: 0 })
		{
			return a;
		}

		var set = new HashSet<string>(a, StringComparer.Ordinal);
		foreach (var tag in b)
		{
			set.Add(tag);
		}

		return [.. set.Order(StringComparer.Ordinal)];
	}

	/// <summary>
	/// Rejects bogus website names like single-character hostnames ("c") from misclassified file:// URIs.
	/// </summary>
	internal static bool IsValidWebsiteName(string name) =>
		name.Length > 1;

	/// <summary>
	/// Sanitizes document names. Windows file paths (e.g. "C:\foo\bar.cs") are normalized to
	/// file:/// URIs. Names already starting with "file:" are left unchanged.
	/// </summary>
	internal static string? SanitizeDocumentName(string? name)
	{
		if (name is null or { Length: 0 })
		{
			return null;
		}

		// Detect Windows-style absolute paths: drive letter followed by :\ or :/
		if (name.Length >= 3
			&& char.IsLetter(name[0])
			&& name[1] == ':'
			&& (name[2] == '\\' || name[2] == '/'))
		{
			return string.Concat("file:///", name.Replace('\\', '/'));
		}

		return name;
	}

	private async Task<List<AppUsageEntry>> GetTopAppsAsync(string startDate, string endDate, CancellationToken ct)
	{
		var dailyUsage = await _usageRepository.GetDailyAppUsageAsync(
			startDate, endDate, cancellationToken: ct).ConfigureAwait(false);
		return BuildTopApps(dailyUsage);
	}

	private async Task<List<WebUsageEntry>> GetTopWebsitesAsync(string startDate, string endDate, CancellationToken ct)
	{
		var dailyWeb = await _usageRepository.GetDailyWebUsageAsync(
			startDate, endDate, cancellationToken: ct).ConfigureAwait(false);

		return dailyWeb
			.Where(d => IsValidWebsiteName(d.Name))
			.GroupBy(d => d.Name, StringComparer.Ordinal)
			.Select(g => new WebUsageEntry
			{
				Name = g.Key,
				TotalMinutes = Math.Round(g.Sum(x => x.TotalSeconds) / 60.0, digits: 1),
			})
			.Where(w => w.TotalMinutes >= 0.5)
			.OrderByDescending(w => w.TotalMinutes)
			.Take(MaxTopWebsites)
			.ToList();
	}

	private async Task<List<WebUsageEntry>> BuildTopWebsitesAggregateAsync(
		string startDate, string endDate, CancellationToken ct)
	{
		return await GetTopWebsitesAsync(startDate, endDate, ct).ConfigureAwait(false);
	}

	private async Task<List<DayOfWeekEntry>> BuildDayOfWeekDistributionAsync(
		string startDate, string endDate, CancellationToken ct)
	{
		var dowUsage = await _usageRepository.GetDayOfWeekAppUsageAsync(
			startDate, endDate, cancellationToken: ct).ConfigureAwait(false);

		return dowUsage
			.GroupBy(d => d.DayOfWeek)
			.Select(g => new DayOfWeekEntry
			{
				DayOfWeek = g.Key,
				TotalMinutes = Math.Round(g.Sum(x => x.TotalSeconds) / 60.0, digits: 1),
			})
			.OrderBy(e => e.DayOfWeek)
			.ToList();
	}

	private static List<DaySummaryEntry> BuildDayEntries(
		IReadOnlyList<Database.Dto.DailyUsageDto> dailyUsage,
		IReadOnlyList<Database.Dto.HourlyUsageDto>? hourlyUsage = null)
	{
		// Pre-compute first/last app per day from hourly data
		var firstLastByDay = new Dictionary<string, (string First, string Last)>(StringComparer.Ordinal);
		if (hourlyUsage is { Count: > 0 })
		{
			foreach (var dayGroup in hourlyUsage.GroupBy(h => h.Day, StringComparer.Ordinal))
			{
				var earliest = dayGroup.OrderBy(h => h.Hour).First();
				var latest = dayGroup.OrderByDescending(h => h.Hour).First();
				firstLastByDay[dayGroup.Key] = (earliest.Name, latest.Name);
			}
		}

		return dailyUsage
			.GroupBy(d => d.Day, StringComparer.Ordinal)
			.Select(g =>
			{
				var totalSec = g.Sum(x => x.TotalSeconds);
				var topApp = g.OrderByDescending(x => x.TotalSeconds).First();
				firstLastByDay.TryGetValue(g.Key, out var fl);
				return new DaySummaryEntry
				{
					Date = g.Key,
					TotalActiveMinutes = Math.Round(totalSec / 60.0, digits: 1),
					TopApp = topApp.Name,
					FirstActivity = fl.First,
					LastActivity = fl.Last,
				};
			})
			.OrderBy(d => d.Date, StringComparer.Ordinal)
			.ToList();
	}

	private static List<AppUsageEntry> BuildTopApps(IReadOnlyList<Database.Dto.DailyUsageDto> dailyUsage)
	{
		return dailyUsage
			.GroupBy(d => d.Name, StringComparer.Ordinal)
			.Select(g => new AppUsageEntry
			{
				Name = g.Key,
				Color = g.First().Color,
				TotalMinutes = Math.Round(g.Sum(x => x.TotalSeconds) / 60.0, digits: 1),
			})
			.OrderByDescending(a => a.TotalMinutes)
			.Take(MaxTopApps)
			.ToList();
	}

	private async Task<List<WebsiteBreakdown>> BuildHourlyWebBreakdownAsync(
		string startDate, string endDate, int limit, CancellationToken ct)
	{
		var hourly = await _usageRepository.GetHourlyWebUsageAsync(
			startDate, endDate, cancellationToken: ct).ConfigureAwait(false);

		return hourly
			.Where(h => IsValidWebsiteName(h.Name))
			.GroupBy(h => h.Name, StringComparer.Ordinal)
			.Select(g => new WebsiteBreakdown
			{
				Name = g.Key,
				TotalMinutes = Math.Round(g.Sum(x => x.TotalSeconds) / 60.0, digits: 1),
				TimeBreakdown = g
					.Select(h => new PeriodBreakdownEntry
					{
						Period = string.Concat(h.Day, " ", h.Hour.ToString("D2", CultureInfo.InvariantCulture), ":00"),
						Minutes = Math.Round(h.TotalSeconds / 60.0, digits: 1),
					})
					.OrderBy(p => p.Period, StringComparer.Ordinal)
					.ToList(),
			})
			.OrderByDescending(w => w.TotalMinutes)
			.Take(limit)
			.ToList();
	}

	private async Task<List<WebsiteBreakdown>> BuildDailyWebBreakdownAsync(
		string startDate, string endDate, int limit, CancellationToken ct)
	{
		var daily = await _usageRepository.GetDailyWebUsageAsync(
			startDate, endDate, cancellationToken: ct).ConfigureAwait(false);

		return daily
			.Where(d => IsValidWebsiteName(d.Name))
			.GroupBy(d => d.Name, StringComparer.Ordinal)
			.Select(g => new WebsiteBreakdown
			{
				Name = g.Key,
				TotalMinutes = Math.Round(g.Sum(x => x.TotalSeconds) / 60.0, digits: 1),
				TimeBreakdown = g
					.Select(d => new PeriodBreakdownEntry
					{
						Period = d.Day,
						Minutes = Math.Round(d.TotalSeconds / 60.0, digits: 1),
					})
					.OrderBy(p => p.Period, StringComparer.Ordinal)
					.ToList(),
			})
			.OrderByDescending(w => w.TotalMinutes)
			.Take(limit)
			.ToList();
	}

	private static DiagnosticsInfo BuildDiagnostics(bool hasCapability) =>
		hasCapability
			? DiagnosticsInfo.Ok
			: new DiagnosticsInfo
			{
				Degraded = true,
				ReasonCode = "FallbackComputation",
				RemediationHint = "Pre-aggregated tables not found. Results computed from raw activities.",
			};

	private static (DateTime Start, DateTime End) ParseDates(string startDate, string endDate)
	{
		var start = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var end = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		return (start, end);
	}

	private static string FormatLocalTime(DateTime dt) =>
		dt.ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
}
#pragma warning restore IL2026
