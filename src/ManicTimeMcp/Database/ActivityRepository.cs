using ManicTimeMcp.Database.Dto;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for ManicTime activity and group data backed by SQLite.</summary>
public sealed class ActivityRepository : IActivityRepository
{
	private readonly IDbConnectionFactory _connectionFactory;
	private readonly ILogger<ActivityRepository> _logger;

	/// <summary>Creates a new activity repository.</summary>
	public ActivityRepository(IDbConnectionFactory connectionFactory, ILogger<ActivityRepository> logger)
	{
		_connectionFactory = connectionFactory;
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
					SELECT ActivityId, TimelineId, StartLocalTime, EndLocalTime, DisplayName, GroupId
					FROM Ar_Activity
					WHERE TimelineId = @timelineId
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
						TimelineId = reader.GetInt64(1),
						StartLocalTime = reader.GetString(2),
						EndLocalTime = reader.GetString(3),
						DisplayName = await reader.IsDBNullAsync(4, ct).ConfigureAwait(false) ? null : reader.GetString(4),
						GroupId = await reader.IsDBNullAsync(5, ct).ConfigureAwait(false) ? null : reader.GetInt64(5),
					});
				}

				_logger.QueryExecuted("GetActivities", results.Count);
				return (IReadOnlyList<ActivityDto>)results.AsReadOnly();
			},
			cancellationToken);
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
					SELECT GroupId, TimelineId, DisplayName, ParentGroupId
					FROM Ar_Group
					WHERE TimelineId = @timelineId
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
						TimelineId = reader.GetInt64(1),
						DisplayName = reader.GetString(2),
						ParentGroupId = await reader.IsDBNullAsync(3, ct).ConfigureAwait(false) ? null : reader.GetInt64(3),
					});
				}

				_logger.QueryExecuted("GetGroups", results.Count);
				return (IReadOnlyList<GroupDto>)results.AsReadOnly();
			},
			cancellationToken);
	}
}
