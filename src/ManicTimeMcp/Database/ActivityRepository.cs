using ManicTimeMcp.Database.Dto;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for ManicTime activity and group data backed by SQLite.</summary>
public sealed class ActivityRepository : IActivityRepository
{
	private readonly IDbConnectionFactory _connectionFactory;
	private readonly QueryCapabilityMatrix _capabilities;
	private readonly ILogger<ActivityRepository> _logger;

	/// <summary>Creates a new activity repository.</summary>
	public ActivityRepository(
		IDbConnectionFactory connectionFactory,
		QueryCapabilityMatrix capabilities,
		ILogger<ActivityRepository> logger)
	{
		_connectionFactory = connectionFactory;
		_capabilities = capabilities;
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(
		long timelineId,
		string startLocalTime,
		string endLocalTime,
		int? limit = null,
		CancellationToken cancellationToken = default)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultActivities, QueryLimits.MaxActivities);

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<ActivityDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT ActivityId, ReportId, StartLocalTime, EndLocalTime, Name, GroupId
					FROM Ar_Activity
					WHERE ReportId = @timelineId
					  AND StartLocalTime < @endLocalTime
					  AND EndLocalTime > @startLocalTime
					ORDER BY StartLocalTime
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@timelineId", timelineId);
				command.Parameters.AddWithValue("@startLocalTime", startLocalTime);
				command.Parameters.AddWithValue("@endLocalTime", endLocalTime);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				var results = new List<ActivityDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new ActivityDto
					{
						ActivityId = reader.GetInt64(0),
						ReportId = reader.GetInt64(1),
						StartLocalTime = reader.GetString(2),
						EndLocalTime = reader.GetString(3),
						Name = await reader.IsDBNullAsync(4, ct).ConfigureAwait(false) ? null : reader.GetString(4),
						GroupId = await reader.IsDBNullAsync(5, ct).ConfigureAwait(false) ? null : reader.GetInt64(5),
					});
				}

				_logger.QueryExecuted("GetActivities", results.Count);
				return (IReadOnlyList<ActivityDto>)results.AsReadOnly();
			},
			cancellationToken);
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<ActivityDto>> GetActivitiesWithGroupTypeAsync(
		long timelineId,
		string startLocalTime,
		string endLocalTime,
		string groupType,
		int? limit = null,
		CancellationToken cancellationToken = default)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultActivities, QueryLimits.MaxActivities);

		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<ActivityDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT a.ActivityId, a.ReportId, a.StartLocalTime, a.EndLocalTime, a.Name, a.GroupId
					FROM Ar_Activity a
					INNER JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
					WHERE a.ReportId = @timelineId
					  AND a.StartLocalTime < @endLocalTime
					  AND a.EndLocalTime > @startLocalTime
					  AND g.GroupType = @groupType
					ORDER BY a.StartLocalTime
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@timelineId", timelineId);
				command.Parameters.AddWithValue("@startLocalTime", startLocalTime);
				command.Parameters.AddWithValue("@endLocalTime", endLocalTime);
				command.Parameters.AddWithValue("@groupType", groupType);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				var results = new List<ActivityDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new ActivityDto
					{
						ActivityId = reader.GetInt64(0),
						ReportId = reader.GetInt64(1),
						StartLocalTime = reader.GetString(2),
						EndLocalTime = reader.GetString(3),
						Name = await reader.IsDBNullAsync(4, ct).ConfigureAwait(false) ? null : reader.GetString(4),
						GroupId = await reader.IsDBNullAsync(5, ct).ConfigureAwait(false) ? null : reader.GetInt64(5),
					});
				}

				_logger.QueryExecuted("GetActivitiesWithGroupType", results.Count);
				return (IReadOnlyList<ActivityDto>)results.AsReadOnly();
			},
			cancellationToken);
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<EnrichedActivityDto>> GetEnrichedActivitiesAsync(
		long timelineId,
		string startLocalTime,
		string endLocalTime,
		int? limit = null,
		CancellationToken cancellationToken = default)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultActivities, QueryLimits.MaxActivities);

		return _capabilities.HasCommonGroup && _capabilities.HasTags
			? GetEnrichedActivitiesFullAsync(timelineId, startLocalTime, endLocalTime, effectiveLimit, cancellationToken)
			: GetEnrichedActivitiesDegradedAsync(timelineId, startLocalTime, endLocalTime, effectiveLimit, cancellationToken);
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<GroupDto>> GetGroupsAsync(long timelineId, CancellationToken cancellationToken = default)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<GroupDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT GroupId, ReportId, Name
					FROM Ar_Group
					WHERE ReportId = @timelineId
					ORDER BY GroupId
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@timelineId", timelineId);
				command.Parameters.AddWithValue("@limit", QueryLimits.MaxGroups);

				var results = new List<GroupDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new GroupDto
					{
						GroupId = reader.GetInt64(0),
						ReportId = reader.GetInt64(1),
						Name = reader.GetString(2),
					});
				}

				_logger.QueryExecuted("GetGroups", results.Count);
				return (IReadOnlyList<GroupDto>)results.AsReadOnly();
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<EnrichedActivityDto>> GetEnrichedActivitiesFullAsync(
		long timelineId, string startLocalTime, string endLocalTime, int effectiveLimit, CancellationToken cancellationToken)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<EnrichedActivityDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT
						a.ActivityId, a.ReportId, a.StartLocalTime, a.EndLocalTime, a.Name, a.GroupId,
						g.Name, g.Color, g.Key,
						cg.Name,
						(SELECT JSON_GROUP_ARRAY(t.Name) FROM Ar_ActivityTag at
						 INNER JOIN Ar_Tag t ON at.TagId = t.TagId
						 WHERE at.ActivityId = a.ActivityId) AS Tags
					FROM Ar_Activity a
					LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
					LEFT JOIN Ar_CommonGroup cg ON a.CommonGroupId = cg.CommonGroupId
					WHERE a.ReportId = @timelineId
					  AND a.StartLocalTime < @endLocalTime
					  AND a.EndLocalTime > @startLocalTime
					ORDER BY a.StartLocalTime
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@timelineId", timelineId);
				command.Parameters.AddWithValue("@startLocalTime", startLocalTime);
				command.Parameters.AddWithValue("@endLocalTime", endLocalTime);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				var results = new List<EnrichedActivityDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new EnrichedActivityDto
					{
						ActivityId = reader.GetInt64(0),
						ReportId = reader.GetInt64(1),
						StartLocalTime = reader.GetString(2),
						EndLocalTime = reader.GetString(3),
						Name = await reader.IsDBNullAsync(4, ct).ConfigureAwait(false) ? null : reader.GetString(4),
						GroupId = await reader.IsDBNullAsync(5, ct).ConfigureAwait(false) ? null : reader.GetInt64(5),
						GroupName = await reader.IsDBNullAsync(6, ct).ConfigureAwait(false) ? null : reader.GetString(6),
						GroupColor = await reader.IsDBNullAsync(7, ct).ConfigureAwait(false) ? null : reader.GetString(7),
						GroupKey = await reader.IsDBNullAsync(8, ct).ConfigureAwait(false) ? null : reader.GetString(8),
						CommonGroupName = await reader.IsDBNullAsync(9, ct).ConfigureAwait(false) ? null : reader.GetString(9),
						Tags = ParseTagsJson(await reader.IsDBNullAsync(10, ct).ConfigureAwait(false) ? null : reader.GetString(10)),
					});
				}

				_logger.QueryExecuted("GetEnrichedActivities(Full)", results.Count);
				return (IReadOnlyList<EnrichedActivityDto>)results.AsReadOnly();
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<EnrichedActivityDto>> GetEnrichedActivitiesDegradedAsync(
		long timelineId, string startLocalTime, string endLocalTime, int effectiveLimit, CancellationToken cancellationToken)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<EnrichedActivityDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT
						a.ActivityId, a.ReportId, a.StartLocalTime, a.EndLocalTime, a.Name, a.GroupId,
						g.Name, g.Color, g.Key
					FROM Ar_Activity a
					LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
					WHERE a.ReportId = @timelineId
					  AND a.StartLocalTime < @endLocalTime
					  AND a.EndLocalTime > @startLocalTime
					ORDER BY a.StartLocalTime
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@timelineId", timelineId);
				command.Parameters.AddWithValue("@startLocalTime", startLocalTime);
				command.Parameters.AddWithValue("@endLocalTime", endLocalTime);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				var results = new List<EnrichedActivityDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new EnrichedActivityDto
					{
						ActivityId = reader.GetInt64(0),
						ReportId = reader.GetInt64(1),
						StartLocalTime = reader.GetString(2),
						EndLocalTime = reader.GetString(3),
						Name = await reader.IsDBNullAsync(4, ct).ConfigureAwait(false) ? null : reader.GetString(4),
						GroupId = await reader.IsDBNullAsync(5, ct).ConfigureAwait(false) ? null : reader.GetInt64(5),
						GroupName = await reader.IsDBNullAsync(6, ct).ConfigureAwait(false) ? null : reader.GetString(6),
						GroupColor = await reader.IsDBNullAsync(7, ct).ConfigureAwait(false) ? null : reader.GetString(7),
						GroupKey = await reader.IsDBNullAsync(8, ct).ConfigureAwait(false) ? null : reader.GetString(8),
					});
				}

				_logger.QueryExecuted("GetEnrichedActivities(Degraded)", results.Count);
				return (IReadOnlyList<EnrichedActivityDto>)results.AsReadOnly();
			},
			cancellationToken);
	}

	private static string[]? ParseTagsJson(string? json)
	{
		if (json is null || string.Equals(json, "[]", StringComparison.Ordinal))
		{
			return null;
		}

		// JSON_GROUP_ARRAY returns ["tag1","tag2"] — parse with trim-safe JsonDocument
		using var doc = System.Text.Json.JsonDocument.Parse(json);
		var array = doc.RootElement;
		if (array.ValueKind != System.Text.Json.JsonValueKind.Array || array.GetArrayLength() == 0)
		{
			return null;
		}

		var tags = new string[array.GetArrayLength()];
		for (var i = 0; i < tags.Length; i++)
		{
			tags[i] = array[i].GetString()!;
		}

		return tags;
	}
}
