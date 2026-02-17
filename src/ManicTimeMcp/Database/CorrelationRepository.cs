using ManicTimeMcp.Database.Dto;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for cross-timeline correlation queries backed by SQLite.</summary>
public sealed class CorrelationRepository : ICorrelationRepository
{
	private readonly IDbConnectionFactory _connectionFactory;
	private readonly QueryCapabilityMatrix _capabilities;
	private readonly ILogger<CorrelationRepository> _logger;

	/// <summary>Creates a new correlation repository.</summary>
	public CorrelationRepository(
		IDbConnectionFactory connectionFactory,
		QueryCapabilityMatrix capabilities,
		ILogger<CorrelationRepository> logger)
	{
		_connectionFactory = connectionFactory;
		_capabilities = capabilities;
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<CorrelatedActivityDto>> GetCorrelatedActivitiesAsync(
		string startLocalTime,
		string endLocalTime,
		int? limit = null,
		CancellationToken cancellationToken = default)
	{
		var effectiveLimit = QueryLimits.Clamp(limit, QueryLimits.DefaultActivities, QueryLimits.MaxActivities);

		return _capabilities.HasCommonGroup
			? GetCorrelatedFullAsync(startLocalTime, endLocalTime, effectiveLimit, cancellationToken)
			: GetCorrelatedDegradedAsync(startLocalTime, endLocalTime, effectiveLimit, cancellationToken);
	}

	private Task<IReadOnlyList<CorrelatedActivityDto>> GetCorrelatedFullAsync(
		string startLocalTime, string endLocalTime, int effectiveLimit, CancellationToken cancellationToken)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<CorrelatedActivityDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT a.StartLocalTime, a.EndLocalTime, a.Name, t.SchemaName,
					       g.Name, g.Color, cg.Name
					FROM Ar_Activity a
					JOIN Ar_Timeline t ON a.ReportId = t.ReportId
					LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
					LEFT JOIN Ar_CommonGroup cg ON a.CommonGroupId = cg.CommonGroupId
					WHERE a.StartLocalTime < @endLocalTime AND a.EndLocalTime > @startLocalTime
					ORDER BY a.StartLocalTime, t.SchemaName
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@startLocalTime", startLocalTime);
				command.Parameters.AddWithValue("@endLocalTime", endLocalTime);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				return await ReadCorrelatedResultsAsync(command, hasCg: true, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private Task<IReadOnlyList<CorrelatedActivityDto>> GetCorrelatedDegradedAsync(
		string startLocalTime, string endLocalTime, int effectiveLimit, CancellationToken cancellationToken)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<CorrelatedActivityDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT a.StartLocalTime, a.EndLocalTime, a.Name, t.SchemaName,
					       g.Name, g.Color
					FROM Ar_Activity a
					JOIN Ar_Timeline t ON a.ReportId = t.ReportId
					LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
					WHERE a.StartLocalTime < @endLocalTime AND a.EndLocalTime > @startLocalTime
					ORDER BY a.StartLocalTime, t.SchemaName
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@startLocalTime", startLocalTime);
				command.Parameters.AddWithValue("@endLocalTime", endLocalTime);
				command.Parameters.AddWithValue("@limit", effectiveLimit);

				return await ReadCorrelatedResultsAsync(command, hasCg: false, ct).ConfigureAwait(false);
			},
			cancellationToken);
	}

	private async Task<IReadOnlyList<CorrelatedActivityDto>> ReadCorrelatedResultsAsync(
		Microsoft.Data.Sqlite.SqliteCommand command, bool hasCg, CancellationToken ct)
	{
		var results = new List<CorrelatedActivityDto>();
		using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
		while (await reader.ReadAsync(ct).ConfigureAwait(false))
		{
			results.Add(new CorrelatedActivityDto
			{
				StartLocalTime = reader.GetString(0),
				EndLocalTime = reader.GetString(1),
				Name = await reader.IsDBNullAsync(2, ct).ConfigureAwait(false) ? null : reader.GetString(2),
				SchemaName = reader.GetString(3),
				GroupName = await reader.IsDBNullAsync(4, ct).ConfigureAwait(false) ? null : reader.GetString(4),
				GroupColor = await reader.IsDBNullAsync(5, ct).ConfigureAwait(false) ? null : reader.GetString(5),
				CommonGroupName = hasCg && !await reader.IsDBNullAsync(6, ct).ConfigureAwait(false) ? reader.GetString(6) : null,
			});
		}

		_logger.QueryExecuted("GetCorrelatedActivities", results.Count);
		return results.AsReadOnly();
	}
}
