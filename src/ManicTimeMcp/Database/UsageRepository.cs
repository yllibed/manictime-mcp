using ManicTimeMcp.Database.Dto;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for pre-aggregated usage data backed by SQLite.</summary>
public sealed class UsageRepository : IUsageRepository
{
	/// <summary>
	/// Known schema name candidates for web/browser timelines.
	/// In most ManicTime installations, web browsing data lives inside the Documents timeline
	/// with <c>Ar_Group.GroupType = "ManicTime/WebSites"</c>. Some versions may also have
	/// dedicated browser timelines with their own schema names.
	/// </summary>
	private static readonly string[] WebSchemaNames = ["ManicTime/Documents", "ManicTime/BrowserUrls", "ManicTime/WebSites"];

	/// <summary>Group type value for web browsing entries within the Documents timeline.</summary>
	private const string WebGroupType = "ManicTime/WebSites";

	/// <summary>Group type value for file/document entries within the Documents timeline.</summary>
	private const string FilesGroupType = "ManicTime/Files";

	private readonly IDbConnectionFactory _connectionFactory;
	private readonly QueryCapabilityMatrix _capabilities;
	private readonly ILogger<UsageRepository> _logger;

	/// <summary>Creates a new usage repository.</summary>
	public UsageRepository(
		IDbConnectionFactory connectionFactory,
		QueryCapabilityMatrix capabilities,
		ILogger<UsageRepository> logger)
	{
		_connectionFactory = connectionFactory;
		_capabilities = capabilities;
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<HourlyUsageDto>> GetHourlyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		_capabilities.HasHourlyUsage
			? GetHourlyUsageAsync("Ar_ActivityByHour", startDay, endDay, limit, cancellationToken)
			: GetHourlyUsageFallbackAsync(["ManicTime/Applications"], startDay, endDay, limit, cancellationToken);

	/// <inheritdoc />
	public Task<IReadOnlyList<HourlyUsageDto>> GetHourlyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		_capabilities.HasHourlyUsage
			? GetHourlyUsageAsync("Ar_ActivityByHour", startDay, endDay, limit, cancellationToken)
			: GetHourlyUsageFallbackAsync(WebSchemaNames, startDay, endDay, limit, cancellationToken, WebGroupType);

	/// <inheritdoc />
	public Task<IReadOnlyList<DailyUsageDto>> GetDailyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		_capabilities.HasPreAggregatedAppUsage
			? GetDailyUsageAsync("Ar_ApplicationByDay", startDay, endDay, limit, cancellationToken)
			: GetDailyUsageFallbackAsync(["ManicTime/Applications"], startDay, endDay, limit, cancellationToken);

	/// <inheritdoc />
	public Task<IReadOnlyList<DailyUsageDto>> GetDailyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		_capabilities.HasPreAggregatedWebUsage
			? GetDailyUsageAsync("Ar_WebSiteByDay", startDay, endDay, limit, cancellationToken)
			: GetDailyUsageFallbackAsync(WebSchemaNames, startDay, endDay, limit, cancellationToken, WebGroupType);

	/// <inheritdoc />
	public Task<IReadOnlyList<DailyUsageDto>> GetDailyDocUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		_capabilities.HasPreAggregatedDocUsage
			? GetDailyUsageAsync("Ar_DocumentByDay", startDay, endDay, limit, cancellationToken)
			: GetDailyUsageFallbackAsync(["ManicTime/Documents"], startDay, endDay, limit, cancellationToken, FilesGroupType);

	/// <inheritdoc />
	public Task<IReadOnlyList<DayOfWeekUsageDto>> GetDayOfWeekAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		_capabilities.HasYearlyUsage
			? GetDayOfWeekFromYearlyAsync(startDay, endDay, limit, cancellationToken)
			: GetDayOfWeekFallbackAsync(startDay, endDay, limit, cancellationToken);

	/// <inheritdoc />
	public Task<IReadOnlyList<TimelineSummaryDto>> GetTimelineSummariesAsync(CancellationToken cancellationToken = default) =>
		_capabilities.HasTimelineSummary
			? GetTimelineSummariesPrimaryAsync(cancellationToken)
			: GetTimelineSummariesFallbackAsync(cancellationToken);

	// ── Primary paths (pre-aggregated tables) ──

	private Task<IReadOnlyList<HourlyUsageDto>> GetHourlyUsageAsync(
		string tableName, string startDay, string endDay, int? limit, CancellationToken cancellationToken)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxHourlyUsageRows);
		var queryName = string.Concat("GetHourlyUsage(", tableName, ")");

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<HourlyUsageDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = $"""
					SELECT h.Day, h.Hour, cg.Name, cg.Color, cg.Key, h.TotalSeconds
					FROM {tableName} h
					INNER JOIN Ar_CommonGroup cg ON h.CommonGroupId = cg.CommonGroupId
					WHERE h.Day >= @startDay AND h.Day < @endDay
					ORDER BY h.Day, h.Hour, h.TotalSeconds DESC
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@startDay", startDay);
				command.Parameters.AddWithValue("@endDay", endDay);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				return await ReadHourlyResultsAsync(command, queryName, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<DailyUsageDto>> GetDailyUsageAsync(
		string tableName, string startDay, string endDay, int? limit, CancellationToken cancellationToken)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxDailyUsageRows);
		var queryName = string.Concat("GetDailyUsage(", tableName, ")");

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<DailyUsageDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = $"""
					SELECT d.Day, cg.Name, cg.Color, cg.Key, d.TotalSeconds
					FROM {tableName} d
					INNER JOIN Ar_CommonGroup cg ON d.CommonGroupId = cg.CommonGroupId
					WHERE d.Day >= @startDay AND d.Day < @endDay
					ORDER BY d.Day, d.TotalSeconds DESC
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@startDay", startDay);
				command.Parameters.AddWithValue("@endDay", endDay);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				return await ReadDailyResultsAsync(command, queryName, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<DayOfWeekUsageDto>> GetDayOfWeekFromYearlyAsync(
		string startDay, string endDay, int? limit, CancellationToken cancellationToken)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxDailyUsageRows);

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<DayOfWeekUsageDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT cg.Name, CAST(strftime('%w', aby.Day) AS INTEGER) AS DayOfWeek,
					       SUM(aby.TotalSeconds) AS TotalSeconds
					FROM Ar_ApplicationByYear aby
					JOIN Ar_CommonGroup cg ON aby.CommonGroupId = cg.CommonGroupId
					WHERE aby.Day >= @startDay AND aby.Day < @endDay
					GROUP BY cg.Name, DayOfWeek
					ORDER BY cg.Name, DayOfWeek
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@startDay", startDay);
				command.Parameters.AddWithValue("@endDay", endDay);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				return await ReadDayOfWeekResultsAsync(command, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<TimelineSummaryDto>> GetTimelineSummariesPrimaryAsync(CancellationToken cancellationToken)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<TimelineSummaryDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT ReportId, StartLocalTime, EndLocalTime
					FROM Ar_TimelineSummary
					ORDER BY ReportId
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@limit", QueryLimits.MaxTimelines);

				var results = new List<TimelineSummaryDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new TimelineSummaryDto
					{
						ReportId = reader.GetInt64(0),
						StartLocalTime = reader.GetString(1),
						EndLocalTime = reader.GetString(2),
					});
				}

				_logger.QueryExecuted("GetTimelineSummaries", results.Count);
				return (IReadOnlyList<TimelineSummaryDto>)results.AsReadOnly();
			},
			cancellationToken);
	}

	// ── Fallback paths (compute from Ar_Activity) ──

	private Task<IReadOnlyList<HourlyUsageDto>> GetHourlyUsageFallbackAsync(
		string[] schemaNames, string startDay, string endDay, int? limit,
		CancellationToken cancellationToken, string? groupType = null)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxHourlyUsageRows);
		var queryName = string.Concat("GetHourlyUsage(Fallback:", string.Join('|', schemaNames), ")");
		var (schemaFilter, schemaParams) = BuildSchemaFilter(schemaNames);

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<HourlyUsageDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();
				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = BuildHourlyFallbackSql(schemaFilter, groupType);
				AddSchemaAndRangeParams(command, schemaParams, startDay, endDay, effectiveLimit, groupType);
				return await ReadHourlyResultsAsync(command, queryName, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<DailyUsageDto>> GetDailyUsageFallbackAsync(
		string[] schemaNames, string startDay, string endDay, int? limit,
		CancellationToken cancellationToken, string? groupType = null)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxDailyUsageRows);
		var queryName = string.Concat("GetDailyUsage(Fallback:", string.Join('|', schemaNames), ")");
		var (schemaFilter, schemaParams) = BuildSchemaFilter(schemaNames);

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<DailyUsageDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();
				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = BuildDailyFallbackSql(schemaFilter, groupType);
				AddSchemaAndRangeParams(command, schemaParams, startDay, endDay, effectiveLimit, groupType);
				return await ReadDailyResultsAsync(command, queryName, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<DayOfWeekUsageDto>> GetDayOfWeekFallbackAsync(
		string startDay, string endDay, int? limit, CancellationToken cancellationToken)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultUsageLimit, QueryLimits.MaxDailyUsageRows);
		string[] schemaNames = ["ManicTime/Applications"];
		var (schemaFilter, schemaParams) = BuildSchemaFilter(schemaNames);

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<DayOfWeekUsageDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();
				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = BuildDayOfWeekFallbackSql(schemaFilter);
				AddSchemaAndRangeParams(command, schemaParams, startDay, endDay, effectiveLimit);
				return await ReadDayOfWeekResultsAsync(command, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<TimelineSummaryDto>> GetTimelineSummariesFallbackAsync(CancellationToken cancellationToken)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<TimelineSummaryDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT ReportId, MIN(StartLocalTime) AS StartLocalTime, MAX(EndLocalTime) AS EndLocalTime
					FROM Ar_Activity
					GROUP BY ReportId
					ORDER BY ReportId
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@limit", QueryLimits.MaxTimelines);

				var results = new List<TimelineSummaryDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new TimelineSummaryDto
					{
						ReportId = reader.GetInt64(0),
						StartLocalTime = reader.GetString(1),
						EndLocalTime = reader.GetString(2),
					});
				}

				_logger.QueryExecuted("GetTimelineSummaries(Fallback)", results.Count);
				return (IReadOnlyList<TimelineSummaryDto>)results.AsReadOnly();
			},
			cancellationToken);
	}

	// ── SQL template builders (keep SQL out of retry lambdas for method length) ──

	private static string BuildHourlyFallbackSql(string schemaFilter, string? groupType = null)
	{
		var groupCondition = groupType is not null ? "\n\t\t      AND g.GroupType = @groupType" : "";
		return $"""
		WITH RECURSIVE
		base AS (
		    SELECT a.StartLocalTime, a.EndLocalTime,
		           COALESCE(g.Name, a.Name, '(unknown)') AS DisplayName,
		           g.Color, g.Key
		    FROM Ar_Activity a
		    LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
		    WHERE a.ReportId IN (
		        SELECT ReportId FROM Ar_Timeline
		        WHERE {schemaFilter}
		    )
		      AND a.EndLocalTime > @startDay
		      AND a.StartLocalTime < @endDay{groupCondition}
		),
		hours AS (
		    SELECT DisplayName, Color, Key,
		           StartLocalTime AS SegStart,
		           MIN(EndLocalTime,
		               strftime('%Y-%m-%d %H:00:00', StartLocalTime, '+1 hour')) AS SegEnd,
		           EndLocalTime
		    FROM base
		    UNION ALL
		    SELECT DisplayName, Color, Key,
		           SegEnd AS SegStart,
		           MIN(EndLocalTime,
		               strftime('%Y-%m-%d %H:00:00', SegEnd, '+1 hour')) AS SegEnd,
		           EndLocalTime
		    FROM hours
		    WHERE SegEnd < EndLocalTime
		)
		SELECT DATE(SegStart) AS Day,
		       CAST(strftime('%H', SegStart) AS INTEGER) AS Hour,
		       DisplayName, Color, Key,
		       SUM((JULIANDAY(SegEnd) - JULIANDAY(SegStart)) * 86400) AS TotalSeconds
		FROM hours
		WHERE DATE(SegStart) >= @startDay AND DATE(SegStart) < @endDay
		GROUP BY Day, Hour, DisplayName, Color, Key
		ORDER BY Day, Hour, TotalSeconds DESC
		LIMIT @limit
		""";
	}

	private static string BuildDailyFallbackSql(string schemaFilter, string? groupType = null)
	{
		var groupCondition = groupType is not null ? "\n\t\t      AND g.GroupType = @groupType" : "";
		return $"""
		WITH base AS (
		    SELECT a.StartLocalTime, a.EndLocalTime,
		           COALESCE(g.Name, a.Name, '(unknown)') AS DisplayName,
		           g.Color, g.Key,
		           DATE(a.StartLocalTime) AS StartDay
		    FROM Ar_Activity a
		    LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
		    WHERE a.ReportId IN (
		        SELECT ReportId FROM Ar_Timeline
		        WHERE {schemaFilter}
		    )
		      AND a.EndLocalTime > @startDay
		      AND a.StartLocalTime < @endDay{groupCondition}
		),
		split AS (
		    SELECT StartDay AS Day, DisplayName, Color, Key,
		           (JULIANDAY(MIN(EndLocalTime, DATE(StartDay, '+1 day')))
		            - JULIANDAY(StartLocalTime)) * 86400 AS Secs
		    FROM base
		    UNION ALL
		    SELECT DATE(StartDay, '+1 day') AS Day, DisplayName, Color, Key,
		           (JULIANDAY(EndLocalTime)
		            - JULIANDAY(DATE(StartDay, '+1 day'))) * 86400 AS Secs
		    FROM base
		    WHERE EndLocalTime > DATE(StartDay, '+1 day')
		)
		SELECT Day, DisplayName, Color, Key, SUM(Secs) AS TotalSeconds
		FROM split
		WHERE Day >= @startDay AND Day < @endDay AND Secs > 0
		GROUP BY Day, DisplayName, Color, Key
		ORDER BY Day, TotalSeconds DESC
		LIMIT @limit
		""";
	}

	private static string BuildDayOfWeekFallbackSql(string schemaFilter) => $"""
		WITH base AS (
		    SELECT a.StartLocalTime, a.EndLocalTime,
		           COALESCE(g.Name, a.Name, '(unknown)') AS DisplayName,
		           DATE(a.StartLocalTime) AS StartDay
		    FROM Ar_Activity a
		    LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
		    WHERE a.ReportId IN (
		        SELECT ReportId FROM Ar_Timeline
		        WHERE {schemaFilter}
		    )
		      AND a.EndLocalTime > @startDay
		      AND a.StartLocalTime < @endDay
		),
		split AS (
		    SELECT StartDay AS Day, DisplayName,
		           (JULIANDAY(MIN(EndLocalTime, DATE(StartDay, '+1 day')))
		            - JULIANDAY(StartLocalTime)) * 86400 AS Secs
		    FROM base
		    UNION ALL
		    SELECT DATE(StartDay, '+1 day') AS Day, DisplayName,
		           (JULIANDAY(EndLocalTime)
		            - JULIANDAY(DATE(StartDay, '+1 day'))) * 86400 AS Secs
		    FROM base
		    WHERE EndLocalTime > DATE(StartDay, '+1 day')
		)
		SELECT DisplayName,
		       CAST(strftime('%w', Day) AS INTEGER) AS DayOfWeek,
		       SUM(Secs) AS TotalSeconds
		FROM split
		WHERE Day >= @startDay AND Day < @endDay AND Secs > 0
		GROUP BY DisplayName, DayOfWeek
		ORDER BY DisplayName, DayOfWeek
		LIMIT @limit
		""";

	private static void AddSchemaAndRangeParams(
		Microsoft.Data.Sqlite.SqliteCommand command,
		(string Name, string Value)[] schemaParams,
		string startDay, string endDay, int effectiveLimit,
		string? groupType = null)
	{
		foreach (var (name, value) in schemaParams)
		{
			command.Parameters.AddWithValue(name, value);
		}
		command.Parameters.AddWithValue("@startDay", startDay);
		command.Parameters.AddWithValue("@endDay", endDay);
		command.Parameters.AddWithValue("@limit", effectiveLimit);
		if (groupType is not null)
		{
			command.Parameters.AddWithValue("@groupType", groupType);
		}
	}

	/// <summary>
	/// Builds a parameterized SQL filter clause for matching multiple schema names.
	/// Returns a WHERE fragment like "(SchemaName = @s0 OR SchemaName = @s1 OR BaseSchemaName = @s0 OR BaseSchemaName = @s1)"
	/// and the associated parameter name/value pairs.
	/// </summary>
	private static (string Filter, (string Name, string Value)[] Params) BuildSchemaFilter(string[] schemaNames)
	{
		var conditions = new string[schemaNames.Length * 2];
		var parameters = new (string Name, string Value)[schemaNames.Length];
		for (var i = 0; i < schemaNames.Length; i++)
		{
			var paramName = $"@s{i}";
			conditions[i] = $"SchemaName = {paramName}";
			conditions[schemaNames.Length + i] = $"BaseSchemaName = {paramName}";
			parameters[i] = (paramName, schemaNames[i]);
		}

		return ($"({string.Join(" OR ", conditions)})", parameters);
	}

	private async Task<IReadOnlyList<DayOfWeekUsageDto>> ReadDayOfWeekResultsAsync(
		Microsoft.Data.Sqlite.SqliteCommand command, CancellationToken ct)
	{
		var results = new List<DayOfWeekUsageDto>();
		using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
		while (await reader.ReadAsync(ct).ConfigureAwait(false))
		{
			results.Add(new DayOfWeekUsageDto
			{
				Name = reader.GetString(0),
				DayOfWeek = reader.GetInt32(1),
				TotalSeconds = reader.GetDouble(2),
			});
		}

		_logger.QueryExecuted("GetDayOfWeekAppUsage", results.Count);
		return results.AsReadOnly();
	}

	// ── Shared reader helpers ──

	private async Task<IReadOnlyList<HourlyUsageDto>> ReadHourlyResultsAsync(
		Microsoft.Data.Sqlite.SqliteCommand command, string queryName, CancellationToken ct)
	{
		var results = new List<HourlyUsageDto>();
		using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
		while (await reader.ReadAsync(ct).ConfigureAwait(false))
		{
			results.Add(new HourlyUsageDto
			{
				Day = reader.GetString(0),
				Hour = reader.GetInt32(1),
				Name = reader.GetString(2),
				Color = await reader.IsDBNullAsync(3, ct).ConfigureAwait(false) ? null : reader.GetString(3),
				Key = await reader.IsDBNullAsync(4, ct).ConfigureAwait(false) ? null : reader.GetString(4),
				TotalSeconds = reader.GetDouble(5),
			});
		}

		_logger.QueryExecuted(queryName, results.Count);
		return results.AsReadOnly();
	}

	private async Task<IReadOnlyList<DailyUsageDto>> ReadDailyResultsAsync(
		Microsoft.Data.Sqlite.SqliteCommand command, string queryName, CancellationToken ct)
	{
		var results = new List<DailyUsageDto>();
		using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
		while (await reader.ReadAsync(ct).ConfigureAwait(false))
		{
			results.Add(new DailyUsageDto
			{
				Day = reader.GetString(0),
				Name = reader.GetString(1),
				Color = await reader.IsDBNullAsync(2, ct).ConfigureAwait(false) ? null : reader.GetString(2),
				Key = await reader.IsDBNullAsync(3, ct).ConfigureAwait(false) ? null : reader.GetString(3),
				TotalSeconds = reader.GetDouble(4),
			});
		}

		_logger.QueryExecuted(queryName, results.Count);
		return results.AsReadOnly();
	}
}
