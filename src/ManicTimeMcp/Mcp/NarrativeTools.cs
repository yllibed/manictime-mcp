using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp.Models;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP tools for narrative and summary queries.</summary>
[McpServerToolType]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class NarrativeTools
{
	private const int MaxSegments = 200;
	private const int MaxTopApps = 50;
	private const int MaxTopWebsites = 50;
	private const int MaxPeriodDays = 31;
	private const int MaxWebsites = 200;

	private readonly IActivityRepository _activityRepository;
	private readonly ITimelineRepository _timelineRepository;
	private readonly IUsageRepository _usageRepository;
	private readonly QueryCapabilityMatrix _capabilities;

	/// <summary>Creates narrative tools with injected repositories.</summary>
	public NarrativeTools(
		IActivityRepository activityRepository,
		ITimelineRepository timelineRepository,
		IUsageRepository usageRepository,
		QueryCapabilityMatrix capabilities)
	{
		_activityRepository = activityRepository;
		_timelineRepository = timelineRepository;
		_usageRepository = usageRepository;
		_capabilities = capabilities;
	}

	/// <summary>Returns a daily summary by delegating to narrative logic.</summary>
	[McpServerTool(Name = "get_daily_summary", ReadOnly = true), Description("Get a structured summary of activity for a specific date, with segments, top applications, and websites.")]
	public async Task<CallToolResult> GetDailySummaryAsync(
		[Description("Date to summarize (ISO-8601, e.g. 2025-01-15)")] string date,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var parsed = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var endDate = parsed.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			var response = await BuildNarrativeAsync(date, endDate, includeWebsites: true, cancellationToken).ConfigureAwait(false);
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
	[McpServerTool(Name = "get_activity_narrative", ReadOnly = true), Description("Get a structured narrative of activities for a date range. Best for single-day 'what did I do?' queries.")]
	public async Task<CallToolResult> GetActivityNarrativeAsync(
		[Description("Start date (ISO-8601, inclusive)")] string startDate,
		[Description("End date (ISO-8601, exclusive)")] string endDate,
		[Description("Include website usage (default true)")] bool includeWebsites = true,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var response = await BuildNarrativeAsync(startDate, endDate, includeWebsites, cancellationToken).ConfigureAwait(false);
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

			var response = await BuildWebsiteUsageAsync(startDate, endDate, rangeDays, limit, cancellationToken).ConfigureAwait(false);
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
		string startDate, string endDate, bool includeWebsites, CancellationToken ct)
	{
		var (start, _) = ParseDates(startDate, endDate);
		var startLocal = FormatLocalTime(start);
		var endLocal = endDate + " 00:00:00";

		var rawSegments = await BuildSegmentsAsync(startLocal, endLocal, ct).ConfigureAwait(false);
		var segments = MergeConsecutiveSegments(rawSegments);
		var totalMinutes = segments.Sum(s => s.DurationMinutes);
		var totalSegments = segments.Count;
		var isTruncated = segments.Count > MaxSegments;
		if (isTruncated)
		{
			segments = segments.Take(MaxSegments).ToList();
		}
		var topApps = await GetTopAppsAsync(startDate, endDate, ct).ConfigureAwait(false);
		var topWebsites = includeWebsites
			? await GetTopWebsitesAsync(startDate, endDate, ct).ConfigureAwait(false)
			: null;

		return new NarrativeResponse
		{
			StartDate = startDate,
			EndDate = endDate,
			TotalActiveMinutes = Math.Round(totalMinutes, digits: 1),
			Segments = segments,
			TopApplications = topApps,
			TopWebsites = topWebsites,
			Truncation = new TruncationInfo
			{
				Truncated = isTruncated,
				ReturnedCount = segments.Count,
				TotalAvailable = totalSegments,
			},
			Diagnostics = BuildDiagnostics(_capabilities.HasPreAggregatedAppUsage),
		};
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
		string startDate, string endDate, int rangeDays, int? limit, CancellationToken ct)
	{
		var effectiveLimit = Math.Min(limit ?? MaxTopWebsites, MaxWebsites);
		var useHourly = rangeDays <= 7;

		var websites = useHourly
			? await BuildHourlyWebBreakdownAsync(startDate, endDate, effectiveLimit, ct).ConfigureAwait(false)
			: await BuildDailyWebBreakdownAsync(startDate, endDate, effectiveLimit, ct).ConfigureAwait(false);

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

		var activities = await _activityRepository.GetEnrichedActivitiesAsync(
			appTimeline.ReportId, startLocal, endLocal,
			limit: QueryLimits.MaxActivities, cancellationToken: ct).ConfigureAwait(false);

		return activities
			.Select(a =>
			{
				var startDt = DateTime.ParseExact(a.StartLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				var endDt = DateTime.ParseExact(a.EndLocalTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				return new NarrativeSegment
				{
					Start = a.StartLocalTime,
					End = a.EndLocalTime,
					DurationMinutes = Math.Round((endDt - startDt).TotalMinutes, digits: 1),
					Application = a.CommonGroupName ?? a.GroupName ?? a.Name,
					ApplicationColor = a.GroupColor,
					Tags = a.Tags is { Length: > 0 } ? a.Tags : null,
					Refs = new SegmentRefs
					{
						TimelineRef = a.ReportId,
						ActivityRef = a.ActivityId,
					},
				};
			})
			.ToList();
	}

	/// <summary>Merges consecutive segments with the same application into single entries.</summary>
	internal static List<NarrativeSegment> MergeConsecutiveSegments(List<NarrativeSegment> segments)
	{
		if (segments.Count <= 1)
		{
			return segments;
		}

		var merged = new List<NarrativeSegment>(segments.Count);
		var current = segments[0];

		for (var i = 1; i < segments.Count; i++)
		{
			var next = segments[i];
			if (string.Equals(current.Application, next.Application, StringComparison.Ordinal))
			{
				// Merge: extend end time, sum duration, union tags, keep first ref
				var endDt = DateTime.ParseExact(next.End, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				var startDt = DateTime.ParseExact(current.Start, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				var mergedTags = MergeTags(current.Tags, next.Tags);
				current = new NarrativeSegment
				{
					Start = current.Start,
					End = next.End,
					DurationMinutes = Math.Round((endDt - startDt).TotalMinutes, digits: 1),
					Application = current.Application,
					ApplicationColor = current.ApplicationColor,
					Tags = mergedTags,
					Refs = current.Refs,
				};
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
			.GroupBy(d => d.Name, StringComparer.Ordinal)
			.Select(g => new WebUsageEntry
			{
				Name = g.Key,
				TotalMinutes = Math.Round(g.Sum(x => x.TotalSeconds) / 60.0, digits: 1),
			})
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
