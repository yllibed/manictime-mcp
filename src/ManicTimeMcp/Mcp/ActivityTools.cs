using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp.Models;
using Microsoft.Data.Sqlite;

namespace ManicTimeMcp.Mcp;

/// <summary>Operations for querying ManicTime activities.</summary>
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class ActivityTools
{
	private readonly IActivityRepository _activityRepository;
	private readonly ITimelineRepository _timelineRepository;
	private readonly IUsageRepository _usageRepository;
	private readonly QueryCapabilityMatrix _capabilities;

	/// <summary>Creates activity tools with injected repositories.</summary>
	public ActivityTools(
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

	/// <summary>Returns activities for a specific timeline within a date range.</summary>
	[Description("Get activities for a specific timeline within a date range. Use timeline list first to find valid timeline IDs.")]
	public async Task<ToolInvocationResult> GetActivitiesAsync(
		[Description("Timeline ID (from timeline list)")] long timelineId,
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results (default 1000, max 5000)")] int? limit = null,
		[Description("Include group details: name, color, key, commonGroupName (default true)")] bool includeGroupDetails = true,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var (startLocal, endLocal) = ParseDateRange(startDate, endDate);
			var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultActivities, QueryLimits.MaxActivities);

			if (includeGroupDetails)
			{
				return await GetEnrichedActivitiesResultAsync(
					timelineId, startDate, endDate, startLocal, endLocal,
					effectiveLimit, cancellationToken).ConfigureAwait(false);
			}

			var activities = await _activityRepository.GetActivitiesAsync(
				timelineId, startLocal, endLocal, limit, cancellationToken).ConfigureAwait(false);

			return ToolResults.Success(JsonSerializer.Serialize(new
			{
				timelineId,
				startDate,
				endDate,
				count = activities.Count,
				activities,
				truncation = new TruncationInfo
				{
					Truncated = activities.Count >= effectiveLimit,
					ReturnedCount = activities.Count,
					TotalAvailable = null as int?,
				},
				diagnostics = DiagnosticsInfo.Ok,
			}, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://resource/health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	/// <summary>Returns computer usage activities (on/off/idle) for a date range.</summary>
	[Description("Get computer usage (on/off/idle/locked) activities for a date range.")]
	public async Task<ToolInvocationResult> GetComputerUsageAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		return await GetActivitiesBySchemaAsync("ManicTime/ComputerUsage", startDate, endDate, limit, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Returns tag activities for a date range.</summary>
	[Description("Get tag/label activities for a date range.")]
	public async Task<ToolInvocationResult> GetTagsAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		return await GetActivitiesBySchemaAsync("ManicTime/Tags", startDate, endDate, limit, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Returns application usage from pre-aggregated tables (or fallback).</summary>
	[Description("Get application usage for a date range, showing daily totals per application with resolved names and colors.")]
	public async Task<ToolInvocationResult> GetApplicationUsageAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results (default 1000, max 2000)")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var (startDay, endDay) = ParseDayRange(startDate, endDate);
			var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxDailyUsageRows);
			var rawUsage = await _usageRepository.GetDailyAppUsageAsync(
				startDay, endDay, effectiveLimit, cancellationToken).ConfigureAwait(false);
			var usage = ProjectToMinutes(rawUsage);

			var degraded = !_capabilities.HasPreAggregatedAppUsage;
			return ToolResults.Success(JsonSerializer.Serialize(new
			{
				startDate,
				endDate,
				count = usage.Count,
				usage,
				truncation = new TruncationInfo
				{
					Truncated = rawUsage.Count >= effectiveLimit,
					ReturnedCount = usage.Count,
					TotalAvailable = null as int?,
				},
				diagnostics = degraded
					? new DiagnosticsInfo
					{
						Degraded = true,
						ReasonCode = "FallbackComputation",
						RemediationHint = "Pre-aggregated tables not found. Results computed from raw activities. Ensure ManicTime has generated aggregation tables.",
					}
					: DiagnosticsInfo.Ok,
			}, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://resource/health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	/// <summary>Returns document usage from pre-aggregated tables (or fallback).</summary>
	[Description("Get document/file usage for a date range, showing daily totals per document with resolved names.")]
	public async Task<ToolInvocationResult> GetDocumentUsageAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results (default 1000, max 2000)")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var (startDay, endDay) = ParseDayRange(startDate, endDate);
			var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxDailyUsageRows);
			var rawUsage = await _usageRepository.GetDailyDocUsageAsync(
				startDay, endDay, effectiveLimit, cancellationToken).ConfigureAwait(false);
			var usage = ProjectToMinutes(rawUsage);

			var degraded = !_capabilities.HasPreAggregatedDocUsage;
			return ToolResults.Success(JsonSerializer.Serialize(new
			{
				startDate,
				endDate,
				count = usage.Count,
				usage,
				truncation = new TruncationInfo
				{
					Truncated = rawUsage.Count >= effectiveLimit,
					ReturnedCount = usage.Count,
					TotalAvailable = null as int?,
				},
				diagnostics = degraded
					? new DiagnosticsInfo
					{
						Degraded = true,
						ReasonCode = "FallbackComputation",
						RemediationHint = "Pre-aggregated tables not found. Results computed from raw activities. Ensure ManicTime has generated aggregation tables.",
					}
					: DiagnosticsInfo.Ok,
			}, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://resource/health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	/// <summary>Returns a consolidated usage summary: apps, websites, documents, tags, and total active time.</summary>
	[Description("Get a consolidated usage summary for a date range. Returns top applications, websites, documents, tags, and total active computer time in a single response.")]
	public async Task<ToolInvocationResult> GetUsageSummaryAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Filter to a specific type: applications, websites, documents, tags, or all")] string type = "all",
		[Description("Maximum items per section (default 1000, max 2000)")] int? limit = null,
		[Description("Minimum minutes to include a website (default 0.5)")] double minMinutes = 0.5,
		CancellationToken cancellationToken = default)
	{
		try
		{
			return await BuildUsageSummaryAsync(startDate, endDate, type, limit, minMinutes, cancellationToken).ConfigureAwait(false);
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://resource/health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	private async Task<ToolInvocationResult> BuildUsageSummaryAsync(
		string startDate, string endDate, string type, int? limit, double minMinutes, CancellationToken cancellationToken)
	{
		var (startDay, endDay) = ParseDayRange(startDate, endDate);
		if (!IsValidUsageSummaryType(type))
		{
			return ToolResults.Error("Invalid usage summary type. Expected one of: all, applications, websites, documents, tags.", "invalid_usage_type");
		}

		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxDailyUsageRows);

		var (appRaw, webRaw, docRaw, tagRaw, usageActivities) = await FetchUsageDataAsync(
			startDate, startDay, endDay, type, effectiveLimit, cancellationToken).ConfigureAwait(false);

		return ToolResults.Success(JsonSerializer.Serialize(new
		{
			startDate,
			endDate,
			totalActiveMinutes = NarrativeTools.ComputeTotalActiveMinutes(usageActivities),
			applications = AggregateToSummary(appRaw, effectiveLimit),
			websites = AggregateToSummary(webRaw, effectiveLimit, minMinutes),
			documents = AggregateToSummary(docRaw, effectiveLimit),
			tags = AggregateToSummary(tagRaw, effectiveLimit),
			diagnostics = !_capabilities.HasPreAggregatedAppUsage && !_capabilities.HasPreAggregatedWebUsage
				? new DiagnosticsInfo
				{
					Degraded = true,
					ReasonCode = "FallbackComputation",
					RemediationHint = "Pre-aggregated tables not found. Results computed from raw activities.",
				}
				: DiagnosticsInfo.Ok,
		}, JsonOptions.Default));
	}

	private async Task<(
		IReadOnlyList<Database.Dto.DailyUsageDto> Apps,
		IReadOnlyList<Database.Dto.DailyUsageDto> Web,
		IReadOnlyList<Database.Dto.DailyUsageDto> Docs,
		IReadOnlyList<Database.Dto.DailyUsageDto> Tags,
		IReadOnlyList<Database.Dto.ActivityDto> Usage)> FetchUsageDataAsync(
		string startDate, string startDay, string endDay, string type, int effectiveLimit, CancellationToken ct)
	{
		var includeAll = string.Equals(type, "all", StringComparison.OrdinalIgnoreCase);
		var empty = Task.FromResult<IReadOnlyList<Database.Dto.DailyUsageDto>>([]);

		var appTask = includeAll || string.Equals(type, "applications", StringComparison.OrdinalIgnoreCase)
			? _usageRepository.GetDailyAppUsageAsync(startDay, endDay, effectiveLimit, ct) : empty;
		var webTask = includeAll || string.Equals(type, "websites", StringComparison.OrdinalIgnoreCase)
			? _usageRepository.GetDailyWebUsageAsync(startDay, endDay, effectiveLimit, ct) : empty;
		var docTask = includeAll || string.Equals(type, "documents", StringComparison.OrdinalIgnoreCase)
			? _usageRepository.GetDailyDocUsageAsync(startDay, endDay, effectiveLimit, ct) : empty;
		var tagTask = includeAll || string.Equals(type, "tags", StringComparison.OrdinalIgnoreCase)
			? _usageRepository.GetDailyTagUsageAsync(startDay, endDay, effectiveLimit, ct) : empty;

		var usageTask = FetchComputerUsageActivitiesAsync(startDate, startDay, endDay, ct);

		await Task.WhenAll(appTask, webTask, docTask, tagTask, usageTask).ConfigureAwait(false);
		return (
			await appTask.ConfigureAwait(false),
			await webTask.ConfigureAwait(false),
			await docTask.ConfigureAwait(false),
			await tagTask.ConfigureAwait(false),
			await usageTask.ConfigureAwait(false));
	}

	private async Task<IReadOnlyList<Database.Dto.ActivityDto>> FetchComputerUsageActivitiesAsync(
		string startDate, string startDay, string endDay, CancellationToken ct)
	{
		var (startLocal, endLocal) = ParseDateRange(startDate, endDay);
		var timelines = await _timelineRepository.GetTimelinesAsync(ct).ConfigureAwait(false);
		var usageTimeline = timelines.FirstOrDefault(t =>
			t.SchemaName.Equals("ManicTime/ComputerUsage", StringComparison.OrdinalIgnoreCase) ||
			t.BaseSchemaName.Equals("ManicTime/ComputerUsage", StringComparison.OrdinalIgnoreCase));

		if (usageTimeline is null)
		{
			return [];
		}

		return await _activityRepository.GetActivitiesAsync(
			usageTimeline.ReportId, startLocal, endLocal, QueryLimits.MaxActivities, ct).ConfigureAwait(false);
	}

	private static List<UsageSummaryEntry> AggregateToSummary(
		IReadOnlyList<Database.Dto.DailyUsageDto> dailyUsage, int limit, double minMinutes = 0)
	{
		return dailyUsage
			.GroupBy(d => d.Name, StringComparer.Ordinal)
			.Select(g => new UsageSummaryEntry
			{
				Name = g.Key,
				Color = g.First().Color,
				TotalMinutes = Math.Round(g.Sum(x => x.TotalSeconds) / 60.0, digits: 1),
			})
			.Where(e => e.TotalMinutes >= minMinutes)
			.OrderByDescending(e => e.TotalMinutes)
			.Take(limit)
			.ToList();
	}

	private static bool IsValidUsageSummaryType(string type) =>
		string.Equals(type, "all", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(type, "applications", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(type, "websites", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(type, "documents", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(type, "tags", StringComparison.OrdinalIgnoreCase);

	private async Task<ToolInvocationResult> GetEnrichedActivitiesResultAsync(
		long timelineId, string startDate, string endDate,
		string startLocal, string endLocal, int effectiveLimit,
		CancellationToken cancellationToken)
	{
		var activities = await _activityRepository.GetEnrichedActivitiesAsync(
			timelineId, startLocal, endLocal, effectiveLimit, cancellationToken).ConfigureAwait(false);

		var degraded = !_capabilities.HasCommonGroup;
		return ToolResults.Success(JsonSerializer.Serialize(new
		{
			timelineId,
			startDate,
			endDate,
			count = activities.Count,
			activities,
			truncation = new TruncationInfo
			{
				Truncated = activities.Count >= effectiveLimit,
				ReturnedCount = activities.Count,
				TotalAvailable = null as int?,
			},
			diagnostics = degraded
				? new DiagnosticsInfo
				{
					Degraded = true,
					ReasonCode = "NoCommonGroup",
					RemediationHint = "CommonGroup table not found. Group details may be incomplete.",
				}
				: DiagnosticsInfo.Ok,
		}, JsonOptions.Default));
	}

	private async Task<ToolInvocationResult> GetActivitiesBySchemaAsync(
		string schemaName, string startDate, string endDate, int? limit,
		CancellationToken cancellationToken)
	{
		try
		{
			var (startLocal, endLocal) = ParseDateRange(startDate, endDate);
			var timelines = await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);

			var matchingTimelines = timelines
				.Where(t => t.SchemaName.Equals(schemaName, StringComparison.OrdinalIgnoreCase) ||
							t.BaseSchemaName.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
				.ToList();

			var allActivities = new List<Database.Dto.ActivityDto>();
			foreach (var timeline in matchingTimelines)
			{
				var activities = await _activityRepository.GetActivitiesAsync(
					timeline.ReportId, startLocal, endLocal, limit, cancellationToken).ConfigureAwait(false);
				allActivities.AddRange(activities);
			}

			var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultActivities, QueryLimits.MaxActivities);
			var sorted = allActivities.OrderBy(a => a.StartLocalTime, StringComparer.Ordinal).ToList();
			var isTruncated = sorted.Count > effectiveLimit;
			if (isTruncated)
			{
				sorted = sorted.Take(effectiveLimit).ToList();
			}

			return ToolResults.Success(JsonSerializer.Serialize(new
			{
				schemaName,
				startDate,
				endDate,
				count = sorted.Count,
				activities = sorted,
				truncation = new TruncationInfo
				{
					Truncated = isTruncated,
					ReturnedCount = sorted.Count,
					TotalAvailable = allActivities.Count,
				},
				diagnostics = DiagnosticsInfo.Ok,
			}, JsonOptions.Default));
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://resource/health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}

	private static (string StartLocal, string EndLocal) ParseDateRange(string startDate, string endDate)
	{
		var start = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var end = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

		return (
			start.ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture),
			end.ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture));
	}

	private static (string StartDay, string EndDay) ParseDayRange(string startDate, string endDate)
	{
		// Validate date format
		_ = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		_ = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		return (startDate, endDate);
	}

	private static List<DailyUsageEntry> ProjectToMinutes(IReadOnlyList<Database.Dto.DailyUsageDto> usage) =>
		usage.Select(u => new DailyUsageEntry
		{
			Day = u.Day,
			Name = u.Name,
			Color = u.Color,
			Key = u.Key,
			TotalMinutes = Math.Round(u.TotalSeconds / 60.0, digits: 1),
		}).ToList();
}
#pragma warning restore IL2026
