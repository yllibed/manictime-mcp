using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ManicTimeMcp.Database;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP tools for querying ManicTime activities.</summary>
[McpServerToolType]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class ActivityTools
{
	private readonly IActivityRepository _activityRepository;
	private readonly ITimelineRepository _timelineRepository;

	/// <summary>Creates activity tools with injected repositories.</summary>
	public ActivityTools(IActivityRepository activityRepository, ITimelineRepository timelineRepository)
	{
		_activityRepository = activityRepository;
		_timelineRepository = timelineRepository;
	}

	/// <summary>Returns activities for a specific timeline within a date range.</summary>
	[McpServerTool(Name = "get_activities", ReadOnly = true), Description("Get activities for a specific timeline within a date range. Use get_timelines first to find valid timeline IDs.")]
	public async Task<string> GetActivitiesAsync(
		[Description("Timeline ID (from get_timelines)")] long timelineId,
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results (default 1000, max 5000)")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		var (startLocal, endLocal) = ParseDateRange(startDate, endDate);

		var activities = await _activityRepository.GetActivitiesAsync(
			timelineId, startLocal, endLocal, limit, cancellationToken).ConfigureAwait(false);

		return JsonSerializer.Serialize(new
		{
			timelineId,
			startDate,
			endDate,
			count = activities.Count,
			isTruncated = activities.Count >= QueryLimits.Clamp(limit, QueryLimits.DefaultActivities, QueryLimits.MaxActivities),
			activities,
		}, JsonOptions.Default);
	}

	/// <summary>Returns computer usage activities (on/off/idle) for a date range.</summary>
	[McpServerTool(Name = "get_computer_usage", ReadOnly = true), Description("Get computer usage (on/off/idle/locked) activities for a date range.")]
	public async Task<string> GetComputerUsageAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		return await GetActivitiesBySchemaAsync("ManicTime/ComputerUsage", startDate, endDate, limit, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Returns tag activities for a date range.</summary>
	[McpServerTool(Name = "get_tags", ReadOnly = true), Description("Get tag/label activities for a date range.")]
	public async Task<string> GetTagsAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		return await GetActivitiesBySchemaAsync("ManicTime/Tags", startDate, endDate, limit, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Returns application usage activities for a date range.</summary>
	[McpServerTool(Name = "get_application_usage", ReadOnly = true), Description("Get application usage activities for a date range, showing which applications were used and when.")]
	public async Task<string> GetApplicationUsageAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		return await GetActivitiesBySchemaAsync("ManicTime/Applications", startDate, endDate, limit, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Returns document usage activities for a date range.</summary>
	[McpServerTool(Name = "get_document_usage", ReadOnly = true), Description("Get document/file usage activities for a date range.")]
	public async Task<string> GetDocumentUsageAsync(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Maximum number of results")] int? limit = null,
		CancellationToken cancellationToken = default)
	{
		return await GetActivitiesBySchemaAsync("ManicTime/Documents", startDate, endDate, limit, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Returns a daily summary aggregating activity across timelines.</summary>
	[McpServerTool(Name = "get_daily_summary", ReadOnly = true), Description("Get a summary of activity for a specific date, including activity counts per timeline.")]
	public async Task<string> GetDailySummaryAsync(
		[Description("Date to summarize (ISO-8601, e.g. 2025-01-15)")] string date,
		CancellationToken cancellationToken = default)
	{
		var parsedDate = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var startLocal = parsedDate.ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
		var endLocal = parsedDate.AddDays(1).ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);

		var timelines = await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);

		var summary = new DailySummary { Date = date };

		foreach (var timeline in timelines)
		{
			var activities = await _activityRepository.GetActivitiesAsync(
				timeline.ReportId, startLocal, endLocal, cancellationToken: cancellationToken).ConfigureAwait(false);

			if (activities.Count == 0)
			{
				continue;
			}

			summary.TimelineSummaries.Add(new TimelineSummaryEntry
			{
				TimelineId = timeline.ReportId,
				SchemaName = timeline.SchemaName,
				ActivityCount = activities.Count,
			});
		}

		return JsonSerializer.Serialize(summary, JsonOptions.Default);
	}

	private async Task<string> GetActivitiesBySchemaAsync(string schemaName, string startDate, string endDate, int? limit, CancellationToken cancellationToken)
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

		return JsonSerializer.Serialize(new
		{
			schemaName,
			startDate,
			endDate,
			count = sorted.Count,
			isTruncated,
			activities = sorted,
		}, JsonOptions.Default);
	}

	private static (string StartLocal, string EndLocal) ParseDateRange(string startDate, string endDate)
	{
		var start = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var end = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

		return (
			start.ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture),
			end.ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture));
	}
}
#pragma warning restore IL2026
