using ManicTimeMcp.Database.Dto;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for environment/device info backed by SQLite.</summary>
public sealed class EnvironmentRepository : IEnvironmentRepository
{
	private readonly IDbConnectionFactory _connectionFactory;
	private readonly ILogger<EnvironmentRepository> _logger;

	/// <summary>Creates a new environment repository.</summary>
	public EnvironmentRepository(IDbConnectionFactory connectionFactory, ILogger<EnvironmentRepository> logger)
	{
		_connectionFactory = connectionFactory;
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<EnvironmentDto>> GetEnvironmentsAsync(CancellationToken cancellationToken = default)
	{
		return SqliteRetryHelper.ExecuteWithRetryAsync<IReadOnlyList<EnvironmentDto>>(
			_logger,
			async ct =>
			{
				ct.ThrowIfCancellationRequested();

				using var connection = _connectionFactory.CreateConnection();
				using var command = connection.CreateCommand();
				command.CommandText = """
					SELECT EnvironmentId, DeviceName
					FROM Ar_Environment
					ORDER BY EnvironmentId
					""";

				var results = new List<EnvironmentDto>();
				using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
				while (await reader.ReadAsync(ct).ConfigureAwait(false))
				{
					results.Add(new EnvironmentDto
					{
						EnvironmentId = reader.GetInt64(0),
						DeviceName = reader.GetString(1),
					});
				}

				_logger.QueryExecuted("GetEnvironments", results.Count);
				return (IReadOnlyList<EnvironmentDto>)results.AsReadOnly();
			},
			cancellationToken);
	}
}
