using ManicTimeMcp.Database.Dto;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for ManicTime timeline data backed by SQLite.</summary>
public sealed class TimelineRepository : ITimelineRepository
{
	private readonly IDbConnectionFactory _connectionFactory;
	private readonly ILogger<TimelineRepository> _logger;

	/// <summary>Creates a new timeline repository.</summary>
	public TimelineRepository(IDbConnectionFactory connectionFactory, ILogger<TimelineRepository> logger)
	{
		_connectionFactory = connectionFactory;
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<TimelineDto>> GetTimelinesAsync(CancellationToken cancellationToken = default)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<TimelineDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT ReportId, SchemaName, BaseSchemaName
					FROM Ar_Timeline
					ORDER BY ReportId
					LIMIT @limit
					""";
				command.Parameters.AddWithValue("@limit", QueryLimits.MaxTimelines);

				var results = new List<TimelineDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new TimelineDto
					{
						ReportId = reader.GetInt64(0),
						SchemaName = reader.GetString(1),
						BaseSchemaName = reader.GetString(2),
					});
				}

				_logger.QueryExecuted("GetTimelines", results.Count);
				return (IReadOnlyList<TimelineDto>)results.AsReadOnly();
			},
			cancellationToken);
	}
}
